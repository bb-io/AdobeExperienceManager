using Apps.AEM.Models.Entities;
using Apps.AEM.Models.Requests;
using Apps.AEM.Models.Responses;
using Apps.AEM.Utils;
using Apps.AEM.Utils.Converters;
using Apps.AEM.Utils.Converters.InteroperableContent;
using Apps.AEM.Utils.Converters.OriginalContent;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Blueprints;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Applications.Sdk.Utils.Extensions.Files;
using Blackbird.Filters.Transformations;
using Blackbird.Filters.Xliff.Xliff2;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System.Text;

namespace Apps.AEM.Actions;

[ActionList("Content")]
public class ContentActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient) : Invocable(invocationContext)
{
    [Action("Search content", Description = "Search for content using specific criteria.")]
    [BlueprintActionDefinition(BlueprintAction.SearchContent)]
    public async Task<SearchContentResponse> SearchContent([ActionParameter] SearchContentRequest input)
    {
        var actionTime = DateTime.UtcNow;
        var searchRequest = ContentSearch.BuildRequest(new()
        {
            RootPath = input.RootPath,
            StartDate = input.StartDate ?? actionTime.AddDays(-365 * 5),
            EndDate = input.EndDate ?? actionTime,
            Tags = input.Tags,
            Keyword = input.Keyword,
            ContentType = input.ContentType,
            Events = input.Events,
            Limit = 100,
        });

        var contentResults = await Client.Paginate<ContentResponse>(searchRequest);

        return new(contentResults);
    }

    [Action("Download content", Description = "Download content as interoperable HTML or the original JSON.")]
    [BlueprintActionDefinition(BlueprintAction.DownloadContent)]
    public async Task<DownloadContentResponse> DownloadContent(
        [ActionParameter] DownloadContentRequest input)
    {
        if (input.ContentId.StartsWith("/content/dam") && (input.ContentId.EndsWith(".dita") || input.ContentId.EndsWith(".ditamap")))
            throw new PluginMisconfigurationException("Use dedicated Guides actions for translating DITA files.");

        if (input.ContentId.StartsWith("/content/dam"))
            throw new PluginMisconfigurationException("Assets which are not DITA files must use dedicated asset actions.");

        if (!input.ContentId.StartsWith("/content/"))
            throw new PluginMisconfigurationException("Only sites content is supported (should start with /content/.");

        var request = new RestRequest("/content/services/bb-aem-connector/content-exporter.json")
            .AddQueryParameter("contentPath", input.ContentId);

        if (input.ExportLiveCopyCancelledInheritance == true)
            request.AddQueryParameter("checkLiveCopy", "true");

        var response = await Client.ExecuteWithErrorHandling(request);
        if (response.Content == null)
            throw new PluginApplicationException("Failed to retrieve content. Response content is null.");

        var (referenceEntities, referenceErrors) = input.IncludeReferenceContent == true
                ? await GetReferenceEntitiesAsync(response.Content, input.SkipReferenceContentPaths ?? [])
                : (new List<ReferenceEntity>(), new List<string>());

        var filename = ContentPathToFilenameConverter.PathToFilename(input.ContentId);

        FileReference outputFile;

        switch (input.FileFormat ?? "text/html")
        {
            case "original":
                var jsonString = OriginalToJsonConverter.ConvertToJson(
                    response.Content,
                    input.ContentId,
                    referenceEntities,
                    input.IncludeReferenceContent == true);

                var jsonMemoryStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonString)) { Position = 0 };

                outputFile = await fileManagementClient.UploadAsync(jsonMemoryStream, "application/json", $"{filename}.json");
                break;

            case "text/html":
                var jsonObj = JsonConvert.DeserializeObject<JObject>(response.Content);
                var metadata = BlackbirdMetadataFactory.Create(Credentials, input.ContentId, jsonObj);
                var htmlString = JsonToHtmlConverter.ConvertToHtml(
                    response.Content,
                    input.ContentId,
                    referenceEntities,
                    metadata);

                var memoryStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(htmlString)) { Position = 0 };

                outputFile = await fileManagementClient.UploadAsync(memoryStream, "text/html", $"{filename}.html");
                break;

            default:
                throw new PluginMisconfigurationException($"Unsupported file format: {input.FileFormat}");
        }

        return new(outputFile, referenceErrors);
    }

    [Action("Upload sites content", Description = "Update content at the specified path, or create it if it does not exist. Accepts a translated file (interoperable HTML or XLIFF) and the original JSON file as input.")]
    [BlueprintActionDefinition(BlueprintAction.UploadContent)]
    public async Task<UploadSitesResponse> UploadContent([ActionParameter] UploadContentRequest input)
    {
        if (!string.IsNullOrWhiteSpace(input.ContentId) && input.SkipUpdatingReferences != true)
            throw new PluginMisconfigurationException("'ContentId' can only be set with 'SkipUpdatingReferences' being set to true, as path overwrite only impacts a main (root) content.");

        var fileStream = await fileManagementClient.DownloadAsync(input.Content);
        var bytes = await fileStream.GetByteData();

        var inputString = System.Text.Encoding.UTF8.GetString(bytes);

        if (Xliff2Serializer.IsXliff2(inputString))
        {
            inputString = Transformation.Parse(inputString, input.Content.Name).Target().Serialize()
                ?? throw new PluginMisconfigurationException("XLIFF did not contain any files");
        }

        if (GuidesActions.IsDita(inputString))
            throw new PluginMisconfigurationException("Use 'Upload guides content' action to upload dita maps and topics.");

        var isJsonInput = OriginalJsonValidator.IsJson(inputString);
        var entities = isJsonInput
            ? JsonToOriginalConverter.ConvertToEntities(inputString)
            : HtmlToJsonConverter.ConvertToJson(inputString);

        UploadContentResponse? rootResult = null;
        var referenceContentUploadResults = new List<UploadContentResponse>();
        foreach (var entity in entities)
        {
            if (entity.ReferenceContent && input.SkipUpdatingReferences == true)
                continue;

            try
            {
                // allow overwriting source path from the action input
                // so that target content can be uploaded to a very different path from source
                var sourcePath = !string.IsNullOrWhiteSpace(input.ContentId) && !entity.ReferenceContent
                    ? input.ContentId
                    : entity.SourcePath;

                var targetPath = ModifyPath(sourcePath, input.SourceLocale, input.Locale);

                foreach (var reference in entity.References)
                {
                    reference.ReferencePath = ModifyPath(reference.ReferencePath, input.SourceLocale, input.Locale);
                }

                var jsonString = JsonConvert.SerializeObject(new
                {
                    sourcePath = entity.SourcePath,
                    targetPath,
                    targetContent = entity.TargetContent,
                    references = entity.References,
                });

                var uploadRequest = new RestRequest("/content/services/bb-aem-connector/content-importer", Method.Post)
                    .AddHeader("Content-Type", "application/json")
                    .AddHeader("Accept", "application/json")
                    .AddStringBody(jsonString, DataFormat.Json);

                var uploadResult = await Client.ExecuteWithErrorHandling<UploadContentResponse>(uploadRequest);
                if (string.IsNullOrEmpty(uploadResult.Message))
                    throw new PluginApplicationException("Failed to upload content. No message returned from server.");
                
                if (!entity.ReferenceContent)
                {
                    rootResult = new UploadContentResponse
                    {
                        ContentId = targetPath,
                        Message = uploadResult.Message.Replace("Content imported successfully", "Content uploaded successfully"),
                    };

                    if (!isJsonInput)
                    {
                        rootResult.TargetFile = await TryDownloadTargetForBlacklakeAsync(entity.SourcePath, targetPath,
                            input.GetCleanTargetLanguage());
                    }
                }
                else
                {
                    referenceContentUploadResults.Add(new()
                    {
                        ContentId = targetPath,
                        Message = uploadResult.Message.Replace(
                            "Content imported successfully",
                            "Content uploaded successfully"),
                    });
                }
            }
            catch (Exception ex)
            {
                if (entity.ReferenceContent && input.IgnoreReferenceContentErrors == true)
                {
                    referenceContentUploadResults.Add(new()
                    {
                        ContentId = entity.SourcePath,
                        Message = $"Ignored error during reference content upload: {ex.Message}",
                    });
                    
                    continue;
                }

                throw;
            }
        }
        
        return new(rootResult!, referenceContentUploadResults);
    }

    [Action("Change tags", Description = "Add or remove tags from content (pages, assets, etc.).")]
    public async Task<ChangeTagsResponse> ChangeTags([ActionParameter] ChangeTagsRequest input)
    {
        if (input.AddTags?.Any() != true && input.RemoveTags?.Any() != true)
            throw new PluginMisconfigurationException("At least one tag to add or to remove must be provided.");

        var updateTagsRequest = new RestRequest("/content/services/bb-aem-connector/update-tags.json", Method.Post)
            .AddJsonBody(new
            {
                contentPath = input.ContentPath,
                addTags = input.AddTags ?? [],
                removeTags = input.RemoveTags ?? [],
            });

        await Client.ExecuteWithErrorHandling(updateTagsRequest);

        var tags = await Client.GetContentTagsAsync([input.ContentPath]);

        return new ChangeTagsResponse
        {
            Tags = tags.GetValueOrDefault(input.ContentPath, []),
        };
    }

    [Action("Get content tags", Description = "Retrieve tags associated with specific content paths.")]
    public async Task<GetContentTagsResponse> GetContentTags([ActionParameter] GetContentTagsRequest input)
    {
        var tags = await Client.GetContentTagsAsync([input.ContentId]);
        return new GetContentTagsResponse
        {
            Tags = tags.GetValueOrDefault(input.ContentId, []),
        };
    }

    [Action("Get content text property", Description = "Get a single string property value from a site page.")]
    public async Task<GetContentTextPropertyResponse> GetContentTextProperty(
     [ActionParameter] GetPropertyValueRequest input)
    {
        var path = BuildJcrContentPath(input.ContentId);
        var request = new RestRequest($"{path}.json", Method.Get);
        var response = await Client.ExecuteWithErrorHandling(request);

        using var jsonDocument = System.Text.Json.JsonDocument.Parse(response.Content);

        if (jsonDocument.RootElement.TryGetProperty(input.PropertyName, out var propertyValue))
        {
            if (propertyValue.ValueKind == System.Text.Json.JsonValueKind.Array)
                throw new PluginApplicationException($"Property '{input.PropertyName}' is an array, not a single text value. Please use 'Get content array property' instead.");

            return new GetContentTextPropertyResponse
            {
                Value = propertyValue.ToString()
            };
        }

        throw new PluginMisconfigurationException($"Property '{input.PropertyName}' not found at {path}.");
    }

    [Action("Get content array property", Description = "Get a multi-value property from a site.")]
    public async Task<GetContentArrayPropertyResponse> GetContentArrayProperty(
    [ActionParameter] GetPropertyValueRequest input)
    {
        var path = BuildJcrContentPath(input.ContentId);
        var request = new RestRequest($"{path}.json", Method.Get);
        var response = await Client.ExecuteWithErrorHandling(request);

        using var jsonDocument = System.Text.Json.JsonDocument.Parse(response.Content);

        if (jsonDocument.RootElement.TryGetProperty(input.PropertyName, out var propertyValue))
        {
            var list = new List<string>();

            if (propertyValue.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                list = propertyValue.EnumerateArray().Select(x => x.ToString()).ToList();
            }
            else
            {
                list.Add(propertyValue.ToString());
            }

            return new GetContentArrayPropertyResponse
            {
                Values = list
            };
        }

        throw new PluginMisconfigurationException($"Property '{input.PropertyName}' not found at {path}.");
    }

    [Action("Update content property", Description = "Updates or creates a property on a site page.")]
    public async Task UpdateContentProperty(
        [ActionParameter] UpdateContentPropertyRequest input)
    {
        var path = BuildJcrContentPath(input.ContentId);

        var tokenRequest = new RestRequest("/libs/granite/csrf/token.json", Method.Get);
        var tokenResponse = await Client.ExecuteWithErrorHandling(tokenRequest);

        using var json = System.Text.Json.JsonDocument.Parse(tokenResponse.Content);
        var csrfToken = json.RootElement.GetProperty("token").GetString();

        var updateRequest = new RestRequest(path, Method.Post);

        updateRequest.AddHeader("CSRF-Token", csrfToken);
        updateRequest.AddHeader("X-Requested-With", "XMLHttpRequest");

        updateRequest.AddParameter("_charset_", "utf-8");
        updateRequest.AddParameter(input.PropertyName, input.PropertyValue);
        updateRequest.AddParameter($"{input.PropertyName}@TypeHint", "String");

        await Client.ExecuteWithErrorHandling(updateRequest);
    }

    [Action("Get IDs from content", Description = "Extracts IDs from the Blackbird generated content file.")]
    public async Task<GetIdsFromContentResponse> GetIdsFromContent([ActionParameter] GetIdsFromContentRequest input)
    {
        using var file = await fileManagementClient.DownloadAsync(input.File);
        using var reader = new StreamReader(file, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var html = await reader.ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(html))
            throw new PluginApplicationException("The content file is empty. Provide an HTML file generated by this AEM app.");

        var document = new HtmlDocument();
        document.LoadHtml(html);

        return new GetIdsFromContentResponse
        {
            RootContentId = ExtractRootContentId(document),
            ReferencedContentIds = ExtractReferencedContentIds(document),
        };
    }

    private static string ExtractRootContentId(HtmlDocument document)
    {
        var rootDiv = document.DocumentNode
            .SelectNodes("//body/div")
            ?.FirstOrDefault(node => string.Equals(node.GetAttributeValue("data-root", string.Empty), "true", StringComparison.OrdinalIgnoreCase));

        var rootContentId = rootDiv?.GetAttributeValue("data-source-path", string.Empty).Trim();

        if (!string.IsNullOrWhiteSpace(rootContentId))
            return rootContentId;

        var metaSourcePath = document.DocumentNode.SelectSingleNode("//meta[@name='blackbird-source-path']");
        rootContentId = metaSourcePath?.GetAttributeValue("content", string.Empty).Trim();

        if (!string.IsNullOrWhiteSpace(rootContentId))
            return rootContentId;

        throw new PluginMisconfigurationException("Couldn't find the root content ID in the HTML file. Make sure the file was generated by the Blackbird's AEM app.");
    }

    private static List<string> ExtractReferencedContentIds(HtmlDocument document)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var referenceDivs = document.DocumentNode.SelectNodes("//body/div[@data-reference-path]");

        if (referenceDivs is null)
            return result.ToList();

        foreach (var referenceDiv in referenceDivs)
        {
            var referencePath = referenceDiv.GetAttributeValue("data-reference-path", string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(referencePath))
                continue;

            result.Add(referencePath);
        }

        return result.ToList();
    }

    private string BuildJcrContentPath(string path)
    {
        if (!path.StartsWith("/content/") || path.StartsWith("/content/dam/"))
            throw new PluginMisconfigurationException("Path must start with /content/ and not be an asset path.");

        return path.EndsWith("/jcr:content")
    ? path
    : $"{path.TrimEnd('/')}/jcr:content";
    }

    private static string ModifyPath(string path, string sourceLanguage, string targetLanguage)
    {
        if (string.IsNullOrWhiteSpace(path) || string.IsNullOrEmpty(sourceLanguage) || string.IsNullOrEmpty(targetLanguage))
        {
            throw new PluginApplicationException("Main (root) rath, source language, and target language must be provided.");
        }

        return path.Replace(sourceLanguage, targetLanguage);
    }

    private async Task<FileReference?> TryDownloadTargetForBlacklakeAsync(string originalSourcePath, string targetPath, string targetLanguage)
    {
        try
        {
            var request = new RestRequest("/content/services/bb-aem-connector/content-exporter.json")
                .AddQueryParameter("contentPath", targetPath);

            var response = await Client.ExecuteWithErrorHandling(request);
            if (string.IsNullOrEmpty(response.Content))
                return null;

            var jsonObj = JsonConvert.DeserializeObject<JObject>(response.Content);
            var metadata = BlackbirdMetadataFactory.Create(Credentials, targetPath, jsonObj, ucidOverride: originalSourcePath, languageOverride: targetLanguage);
            var htmlString = JsonToHtmlConverter.ConvertToHtml(response.Content, targetPath, [], metadata);

            var filename = ContentPathToFilenameConverter.PathToFilename(targetPath);
            var memoryStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(htmlString)) { Position = 0 };
            return await fileManagementClient.UploadAsync(memoryStream, "text/html", $"{filename}.html");
        }
        catch
        {
            return null;
        }
    }

    private async Task<(IEnumerable<ReferenceEntity> entities, IEnumerable<string> errors)> GetReferenceEntitiesAsync(
        string content,
        IEnumerable<string> skipReferenceContentPaths)
    {
        var processedPaths = new HashSet<string>();
        return await ProcessReferencesRecursivelyAsync(content, skipReferenceContentPaths, processedPaths, 0);
    }

    private async Task<(IEnumerable<ReferenceEntity> entities, IEnumerable<string> errors)> ProcessReferencesRecursivelyAsync(
        string content,
        IEnumerable<string> skipReferenceContentPaths,
        ISet<string> processedPaths,
        int depth,
        int maxDepth = 100)
    {
        var entities = new List<ReferenceEntity>();
        var errors = new List<string>();

        if (depth > maxDepth || string.IsNullOrEmpty(content))
            return new(entities, errors);

        var references = JsonToHtmlConverter.ExtractReferences(content);
        foreach (var reference in references)
        {
            if (string.IsNullOrEmpty(reference.ReferencePath) || processedPaths.Contains(reference.ReferencePath))
            {
                continue;
            }

            var shouldSkip = skipReferenceContentPaths.Any(skipPath => reference.ReferencePath.Contains(skipPath, StringComparison.OrdinalIgnoreCase));
            if (shouldSkip)
            {
                continue;
            }

            processedPaths.Add(reference.ReferencePath);
            var referenceRequest = new RestRequest("/content/services/bb-aem-connector/content-exporter.json")
                .AddQueryParameter("contentPath", reference.ReferencePath);

            string referenceContent;

            try
            {
                var referenceResponse = await Client.ExecuteWithErrorHandling(referenceRequest);
                referenceContent = referenceResponse.Content ?? string.Empty;
            }
            catch (Exception)
            {
                errors.Add($"{reference.ReferencePath}");
                continue;
            }

            if (!string.IsNullOrWhiteSpace(referenceContent))
            {
                entities.Add(new ReferenceEntity(
                    reference.ReferencePath,
                    referenceContent,
                    reference.PropertyName,
                    reference.PropertyPath));

                var recursiveResult = await ProcessReferencesRecursivelyAsync(
                    referenceContent,
                    skipReferenceContentPaths,
                    processedPaths,
                    depth + 1,
                    maxDepth);

                entities.AddRange(recursiveResult.entities);
                errors.AddRange(recursiveResult.errors);
            }
        }

        return (entities, errors);
    }
}
