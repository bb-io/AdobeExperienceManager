using Apps.AEM.Models.Dtos;
using Apps.AEM.Models.Requests;
using Apps.AEM.Models.Responses;
using Apps.AEM.Utils.Converters;
using Apps.AEM.Utils.Converters.InteroperableContent;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Filters.Extensions;
using Blackbird.Filters.Transformations;
using Blackbird.Filters.Xliff.Xliff2;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System.Text;

namespace Apps.AEM.Actions;

[ActionList("Content fragments")]
public class ContentFragmentActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient)
    : Invocable(invocationContext)
{
    private const string ContentDamRoot = "/content/dam";
    private const string FragmentsEndpoint = "/adobe/sites/cf/fragments";
    private const int DefaultSearchMaxItems = 100;
    private const int MaxPageSize = 50;

    [Action("Search content fragments (experimental)", Description = "Search for content fragments by DAM path prefix and tags.")]
    public async Task<SearchContentFragmentResponse> SearchContentFragments([ActionParameter] SearchContentFragmentsRequest input)
    {
        var rootPath = string.IsNullOrWhiteSpace(input.RootPath)
            ? ContentDamRoot
            : input.RootPath.Trim();

        ValidateDamPath(rootPath);

        var maxItems = input.MaxItems is > 0 ? input.MaxItems.Value : DefaultSearchMaxItems;
        var fragments = await SearchFragmentsAsync(rootPath, input.Tags, maxItems);

        return new SearchContentFragmentResponse(fragments.Select(MapSearchItem));
    }

    [Action("Download content fragment (experimental)", Description = "Download a content fragment's master fields as interoperable HTML.")]
    public async Task<DownloadContentFragmentResponse> DownloadContentFragments([ActionParameter] DownloadContentFragmentRequest input)
    {
        ValidateDamPath(input.ContentId);

        var fragmentLookup = await FindFragmentByPathAsync(input.ContentId);

        if (input.CheckOut == true)
            await CheckOutContentFragment(new() { ContentId = input.ContentId });

        var fragment = await GetFragmentAsync(fragmentLookup.Id);
        

        var html = ContentFragmentHtmlConverter.ConvertToHtml(fragment.Fields, fragment.Path, input.ExcludedFields);
        var fileName = ContentPathToFilenameConverter.PathToFilename(fragment.Path);

        var fileReference = await fileManagementClient.UploadAsync(
            new MemoryStream(Encoding.UTF8.GetBytes(html)) { Position = 0 },
            "text/html",
            $"{fileName}.html");

        return new DownloadContentFragmentResponse(fileReference);
    }

    [Action("Check out content fragment (experimental)", Description = "Check out a content fragment so it can be edited exclusively by the current user.")]
    public async Task<ContentFragmentCheckoutStateResponse> CheckOutContentFragment([ActionParameter] ContentFragmentPathRequest input)
    {
        ValidateDamPath(input.ContentId);

        var fragmentLookup = await FindFragmentByPathAsync(input.ContentId);
        await UpdateFragmentCheckoutStateAsync(fragmentLookup.Id, true);

        return new ContentFragmentCheckoutStateResponse
        {
            ContentId = fragmentLookup.Path,
            CheckedOut = true,
            Message = "Content fragment checked out successfully."
        };
    }

    [Action("Check in content fragment (experimental)", Description = "Check in a content fragment to release its lock. If it is not checked out, the request succeeds without changes.")]
    public async Task<ContentFragmentCheckoutStateResponse> CheckInContentFragment([ActionParameter] ContentFragmentPathRequest input)
    {
        ValidateDamPath(input.ContentId);

        var fragmentLookup = await FindFragmentByPathAsync(input.ContentId);
        await UpdateFragmentCheckoutStateAsync(fragmentLookup.Id, false);

        return new ContentFragmentCheckoutStateResponse
        {
            ContentId = fragmentLookup.Path,
            CheckedOut = false,
            Message = "Content fragment checked in successfully."
        };
    }

    [Action("Upload content fragment (experimental)", Description = "Create or update a translated content fragment variation from HTML or XLIFF.")]
    public async Task<UploadContentFragmentResponse> UploadContentFragments([ActionParameter] UploadContentFragmentRequest input)
    {
        if (string.IsNullOrWhiteSpace(input.VariationTitle))
            throw new PluginMisconfigurationException("'Variation title' is required.");

        var fileStream = await fileManagementClient.DownloadAsync(input.Content);
        var inputString = await fileStream.ReadString();

        if (Xliff2Serializer.IsXliff2(inputString))
        {
            inputString = Transformation.Parse(inputString, input.Content.Name).Target().Serialize()
                ?? throw new PluginMisconfigurationException("XLIFF did not contain any files");
        }

        var entities = HtmlToJsonConverter.ConvertToJson(inputString);
        var rootEntity = entities.SingleOrDefault(entity => !entity.ReferenceContent)
            ?? throw new PluginMisconfigurationException("The uploaded file did not contain a root content fragment payload.");

        var fields = rootEntity.TargetContent["fields"] as JArray
            ?? throw new PluginMisconfigurationException("The uploaded file did not contain content fragment fields.");

        var targetPath = !string.IsNullOrWhiteSpace(input.ContentId)
            ? input.ContentId
            : rootEntity.SourcePath;

        if (string.IsNullOrWhiteSpace(targetPath))
            throw new PluginMisconfigurationException("The uploaded file did not contain a source content fragment path.");

        ValidateDamPath(targetPath);

        var fragmentLookup = await FindFragmentByPathAsync(targetPath);

        try
        {
            await UpdateFragmentCheckoutStateAsync(fragmentLookup.Id, true);
            var matchingVariation = await FindVariationByTitleAsync(fragmentLookup.Id, input.VariationTitle);

            ContentFragmentVariationDto variation;
            string etag;
            var isCreated = false;

            if (matchingVariation != null)
            {
                (variation, etag) = await GetVariationAsync(fragmentLookup.Id, matchingVariation.Name);
            }
            else
            {
                (variation, etag) = await CreateVariationAsync(fragmentLookup.Id, input.VariationTitle, input.VariationDescription);
                isCreated = true;
            }

            await PatchVariationFieldsAsync(fragmentLookup.Id, variation.Name, etag, fields);

            return new UploadContentFragmentResponse
            {
                ContentId = fragmentLookup.Path,
                VariationName = variation.Name,
                Message = isCreated
                    ? "Content fragment variation created and uploaded successfully."
                    : "Content fragment variation uploaded successfully."
            };
        }
        finally
        {
            if (input.CheckIn == true)
            {
                await UpdateFragmentCheckoutStateAsync(fragmentLookup.Id, false);
            }
        }
    }

    private static string BuildSearchQuery(string rootPath, IEnumerable<string>? modelTags)
    {
        var filter = new JObject
        {
            ["path"] = rootPath
        };

        var requestedModelTags = modelTags?
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (requestedModelTags?.Length > 0)
        {
            filter["modelTags"] = new JArray(requestedModelTags);
        }

        var query = new JObject
        {
            ["filter"] = filter
        };

        return query.ToString(Formatting.None);
    }

    private static ContentFragmentItemResponse MapSearchItem(ContentFragmentDto fragment)
    {
        return new ContentFragmentItemResponse
        {
            ContentId = fragment.Path,
            FragmentId = fragment.Id,
            Title = fragment.Title,
            ModelName = fragment.Model?.Title ?? fragment.Model?.Name ?? string.Empty,
            Status = fragment.Status,
            Created = fragment.Created?.At,
            Modified = fragment.Modified?.At
        };
    }

    private static void ValidateDamPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !path.StartsWith(ContentDamRoot, StringComparison.OrdinalIgnoreCase))
            throw new PluginMisconfigurationException("Content fragment path must start with /content/dam.");
    }

    private async Task<ContentFragmentDto> FindFragmentByPathAsync(string path)
    {
        var fragment = await TryFindFragmentByPathAsync(path, path);
        if (fragment != null)
        {
            return fragment;
        }

        if (TryGetRelativeDamPath(path, out var relativePath))
        {
            fragment = await TryFindFragmentByPathAsync(relativePath, path);
            if (fragment != null)
            {
                return fragment;
            }
        }

        throw new PluginMisconfigurationException($"No content fragment was found at path '{path}'.");
    }

    private async Task<List<ContentFragmentDto>> SearchFragmentsAsync(string rootPath, IEnumerable<string>? tags, int maxItems)
    {
        var request = new RestRequest($"{FragmentsEndpoint}/search")
            .AddQueryParameter("limit", Math.Min(maxItems, MaxPageSize))
            .AddQueryParameter("projection", "summary")
            .AddQueryParameter("query", BuildSearchQuery(rootPath, tags));

        return await Client.PaginateByCursor<ContentFragmentDto>(request, maxItems);
    }

    private async Task<ContentFragmentDto> GetFragmentAsync(string fragmentId)
    {
        var (fragment, _) = await GetFragmentWithEtagAsync(fragmentId);
        return fragment;
    }

    private async Task<(ContentFragmentDto fragment, string etag)> GetFragmentWithEtagAsync(string fragmentId)
    {
        var request = new RestRequest($"{FragmentsEndpoint}/{fragmentId}")
            .AddQueryParameter("references", "none");

        var response = await Client.ExecuteWithErrorHandling(request);
        var fragment = DeserializeResponse<ContentFragmentDto>(
            response,
            $"Failed to retrieve content fragment '{fragmentId}'.");

        var etag = response.Headers?
            .FirstOrDefault(header => string.Equals(header.Name?.ToString(), "ETag", StringComparison.OrdinalIgnoreCase))
            ?.Value?.ToString()
            ?? fragment.Etag
            ?? throw new PluginApplicationException("The AEM response did not include an ETag header.");

        return (fragment, etag);
    }

    private async Task<ContentFragmentVariationDto?> FindVariationByTitleAsync(string fragmentId, string variationTitle)
    {
        var request = new RestRequest($"{FragmentsEndpoint}/{fragmentId}/variations")
            .AddQueryParameter("limit", MaxPageSize);

        var variations = await Client.PaginateByCursor<ContentFragmentVariationDto>(request, 500);
        var matches = variations
            .Where(variation => string.Equals(variation.Title, variationTitle, StringComparison.Ordinal))
            .ToList();

        return matches.Count switch
        {
            0 => null,
            1 => matches[0],
            _ => throw new PluginApplicationException(
                $"Multiple content fragment variations matched the title '{variationTitle}'. Use a unique variation title.")
        };
    }

    private async Task<(ContentFragmentVariationDto variation, string etag)> GetVariationAsync(string fragmentId, string variationName)
    {
        var request = new RestRequest($"{FragmentsEndpoint}/{fragmentId}/variations/{Uri.EscapeDataString(variationName)}")
            .AddQueryParameter("references", "none");

        var response = await Client.ExecuteWithErrorHandling(request);
        var variation = DeserializeResponse<ContentFragmentVariationDto>(
            response,
            $"Failed to retrieve variation '{variationName}'.");

        return (variation, GetEtag(response));
    }

    private async Task<(ContentFragmentVariationDto variation, string etag)> CreateVariationAsync(
        string fragmentId,
        string variationTitle,
        string? variationDescription)
    {
        var payload = new JObject
        {
            ["title"] = variationTitle
        };

        if (!string.IsNullOrWhiteSpace(variationDescription))
        {
            payload["description"] = variationDescription;
        }

        var request = new RestRequest($"{FragmentsEndpoint}/{fragmentId}/variations", Method.Post)
            .AddHeader("Content-Type", "application/json")
            .AddHeader("Accept", "application/json")
            .AddStringBody(payload.ToString(Formatting.None), DataFormat.Json);

        var response = await Client.ExecuteWithErrorHandling(request);
        var variation = DeserializeResponse<ContentFragmentVariationDto>(
            response,
            $"Failed to create variation '{variationTitle}'.");

        return (variation, GetEtag(response));
    }

    private async Task PatchVariationFieldsAsync(string fragmentId, string variationName, string etag, JArray fields)
    {
        if (string.IsNullOrWhiteSpace(etag))
            throw new PluginApplicationException("Failed to update variation because the current ETag is missing.");

        var patchPayload = new JArray
        {
            new JObject
            {
                ["op"] = "replace",
                ["path"] = "/fields",
                ["value"] = fields.DeepClone()
            }
        };

        var request = new RestRequest($"{FragmentsEndpoint}/{fragmentId}/variations/{Uri.EscapeDataString(variationName)}", Method.Patch)
            .AddHeader("If-Match", etag)
            .AddHeader("Content-Type", "application/json-patch+json")
            .AddHeader("Accept", "application/json")
            .AddStringBody(patchPayload.ToString(Formatting.None), DataFormat.Json);

        await Client.ExecuteWithErrorHandling(request);
    }

    private async Task UpdateFragmentCheckoutStateAsync(string fragmentId, bool checkedOut)
    {
        var (_, etag) = await GetFragmentWithEtagAsync(fragmentId);
        var payload = new JObject
        {
            ["checkedOut"] = checkedOut
        };

        var request = new RestRequest($"{FragmentsEndpoint}/{fragmentId}/checkoutState", Method.Patch)
            .AddHeader("If-Match", etag)
            .AddHeader("Content-Type", "application/json")
            .AddHeader("Accept", "application/json")
            .AddStringBody(payload.ToString(Formatting.None), DataFormat.Json);

        try
        {
            await Client.ExecuteWithErrorHandling(request);
        }
        catch (PluginApplicationException ex) when (checkedOut && ex.Message.Contains("Status code: 409", StringComparison.OrdinalIgnoreCase))
        {
            throw new PluginMisconfigurationException(
                "The content fragment is currently checked out by another user or process. Check it in before continuing.");
        }
    }

    private static string GetEtag(RestResponse response)
    {
        return (response.Headers ?? [])
                   .FirstOrDefault(header => string.Equals(header.Name?.ToString(), "ETag", StringComparison.OrdinalIgnoreCase))
                   ?.Value?.ToString()
               ?? throw new PluginApplicationException("The AEM response did not include an ETag header.");
    }

    private static T DeserializeResponse<T>(RestResponse response, string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(response.Content))
            throw new PluginApplicationException(errorMessage);

        return JsonConvert.DeserializeObject<T>(response.Content)
               ?? throw new PluginApplicationException(errorMessage);
    }

    private async Task<ContentFragmentDto?> TryFindFragmentByPathAsync(string requestPath, string expectedAbsolutePath)
    {
        var request = new RestRequest(FragmentsEndpoint)
            .AddQueryParameter("path", requestPath)
            .AddQueryParameter("projection", "summary")
            .AddQueryParameter("limit", 2);

        var response = await Client.ExecuteWithErrorHandling<CursorPaginationDto<ContentFragmentDto>>(request);
        var exactMatches = response.Items
            .Where(item => string.Equals(item.Path, expectedAbsolutePath, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (exactMatches.Count == 1)
        {
            return exactMatches[0];
        }

        if (response.Items.Any(item => item.Path.StartsWith(expectedAbsolutePath.TrimEnd('/') + "/", StringComparison.OrdinalIgnoreCase)))
            throw new PluginMisconfigurationException($"Path '{expectedAbsolutePath}' points to a folder. Provide an exact content fragment path.");

        return null;
    }

    private static bool FragmentMatchesAnyTag(ContentFragmentDto fragment, IEnumerable<string> requestedTags)
    {
        var requestedTagSet = new HashSet<string>(requestedTags, StringComparer.OrdinalIgnoreCase);

        if (fragment.Fields
                .OfType<JObject>()
                .Where(field => string.Equals(field["type"]?.ToString(), "tag", StringComparison.OrdinalIgnoreCase))
                .SelectMany(field => field["values"]?.Values<string>() ?? [])
                .Any(tag => tag is not null && requestedTagSet.Contains(tag)))
        {
            return true;
        }

        if (fragment.Tags
                .OfType<JObject>()
                .Select(tag => tag["id"]?.ToString())
                .Where(tagId => !string.IsNullOrWhiteSpace(tagId))
                .Any(tagId => requestedTagSet.Contains(tagId!)))
        {
            return true;
        }

        return fragment.FieldTags
            .OfType<JObject>()
            .Select(tag => tag["id"]?.ToString())
            .Where(tagId => !string.IsNullOrWhiteSpace(tagId))
            .Any(tagId => requestedTagSet.Contains(tagId!));
    }

    private static string GetRelativeDamPath(string path)
    {
        if (!path.StartsWith(ContentDamRoot, StringComparison.OrdinalIgnoreCase))
            throw new PluginMisconfigurationException("Content fragment path must start with /content/dam.");

        var relativePath = path.Length == ContentDamRoot.Length
            ? "/"
            : path.Substring(ContentDamRoot.Length);

        if (string.IsNullOrWhiteSpace(relativePath))
        {
            relativePath = "/";
        }

        return relativePath;
    }

    private static bool TryGetRelativeDamPath(string path, out string relativePath)
    {
        relativePath = string.Empty;

        if (!path.StartsWith(ContentDamRoot, StringComparison.OrdinalIgnoreCase))
            return false;

        relativePath = GetRelativeDamPath(path);
        return true;
    }
}
