using Apps.AEM.Models.Dtos;
using Apps.AEM.Models.Requests;
using Apps.AEM.Models.Responses;
using Apps.AEM.Utils.Converters;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Filters.Transformations;
using Blackbird.Filters.Xliff.Xliff2;
using RestSharp;
using System.Linq;
using System.Text;

namespace Apps.AEM.Actions;

[ActionList("Guides")]
public class GuidesActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient) : Invocable(invocationContext)
{
    [Action("Search referenced content", Description = "Finds references from books and chapters (ditamaps).")]
    public async Task<SearchMappedContentResponse> SearchReferencedContent([ActionParameter] SearchMappedContentRequest input)
    {
        var context = new SearchContext();
        input.ExcludedTags ??= [];

        foreach (var rootId in input.ContentIds)
        {
            // Filter out by tags if chosen
            if (input.ExcludedTags?.Any() == true)
            {
                var rootTags = await Client.GetDamAssetTagsAsync(rootId);

                if (rootTags[rootId].Intersect(input.ExcludedTags).Any())
                    continue; // If excluded, we do not add to results and do not process its references
            }

            var stack = new Stack<string>();
            stack.Push(rootId);

            while (stack.Count > 0)
            {
                var currentPath = stack.Pop();

                if (!ValidateDitaPath(currentPath, context.Errors))
                    continue;

                // Prevent infinite loops
                if (context.Visited.Contains(currentPath))
                    continue;
                context.Visited.Add(currentPath);

                // Return early on dita files as they won't have references
                if (currentPath.EndsWith(".dita"))
                {
                    context.Results.Add(currentPath);
                    continue;
                }
                
                if (input.IncludeMaps == true)
                    context.Results.Add(currentPath);

                // Fetch referenced GUIDs and resolve their paths
                var references = await FetchGuidReferences(currentPath, context.Errors);

                if (references.Count == 0)
                    continue;

                var matchingPaths = await ResolveReferencesToPaths(references, currentPath, input.ExcludedTags ?? [], context.Errors);

                if (input.SearchRecursively != true)
                {
                    // Just add valid children to results
                    foreach (var path in matchingPaths)
                    {
                        context.Results.Add(path);
                        context.Visited.Add(path); // Mark visited to avoid re-processing if duplicates exist
                    }
                }
                else
                {
                    // Recursive search: add references to stack
                    // so they would be collected before checking next input's path
                    var newPaths = matchingPaths
                        .Where(p => !context.Visited.Contains(p))
                        .Reverse(); // Reverse order so they are popped in order returned by AEM and a content is read (aka depth-first)

                    foreach (var path in newPaths)
                        stack.Push(path);
                }
            }
        }

        return new SearchMappedContentResponse
        {
            ContentIds = context.Results.Distinct(),
            Errors = context.Errors.Distinct()
        };
    }

    [Action("Download guides file", Description = "Download's a DITA or a ditamap file.")]
    public async Task<DownloadContentResponse> DownloadDitaContent([ActionParameter] DownloadDitaContentRequest input)
    {
        var request = new RestRequest("/content/services/bb-aem-connector/dita-file-exporter.xml")
            .AddQueryParameter("contentPath", input.ContentId);

        var response = await Client.ExecuteWithErrorHandling(request);

        if (response.Content == null)
            throw new PluginApplicationException("Failed to retrieve content. Response content is null.");

        var rootFile = await fileManagementClient.UploadAsync(
            new MemoryStream(Encoding.UTF8.GetBytes(response.Content)) { Position = 0 },
            "application/xml",
            ContentPathToFilenameConverter.PathToFilename(input.ContentId));

        return new(rootFile, []);
    }

    [Action("Upload guides file", Description = "Upload translation of a DITA file. Accepts translated dita, ditamap or XLIFF file types.")]
    public async Task<UploadContentResponse> UploadDitaContent([ActionParameter] UploadDitaContentRequest input)
    {
        var fileStream = await fileManagementClient.DownloadAsync(input.Content);
        using var reader = new StreamReader(fileStream, Encoding.UTF8);
        var inputString = await reader.ReadToEndAsync();

        if (Xliff2Serializer.IsXliff2(inputString))
        {
            inputString = Transformation.Parse(inputString, input.Content.Name).Target().Serialize()
                ?? throw new PluginMisconfigurationException("XLIFF did not contain any files");
        }

        if (!IsDita(inputString))
            throw new PluginMisconfigurationException("Cannot detect DITA file. Use dedicated action to upload sites content or assets.");

        var request = new RestRequest("/content/services/bb-aem-connector/dita-file-importer.xml", Method.Post)
            .AddHeader("Content-Type", "application/xml")
            .AddHeader("Accept", "application/xml")
            .AddStringBody(inputString, DataFormat.Xml)
            .AddQueryParameter("sourcePath", input.SourceFilePath)
            .AddQueryParameter("targetPath", input.TargetFilePath);

        var uploadResult = await Client.ExecuteWithErrorHandling<UploadContentResponse>(request);

        if (string.IsNullOrEmpty(uploadResult?.Message))
            throw new PluginApplicationException("Failed to upload content. No message returned from server.");

        return uploadResult;
    }

    private static bool ValidateDitaPath(string path, List<string> errors)
    {
        if (!path.StartsWith("/content/dam/"))
        {
            errors.Add($"Content ID: {path} is not a dita file within assets.");
            return false;
        }

        if (!path.EndsWith(".dita") && !path.EndsWith(".ditamap"))
        {
            errors.Add($"Content ID: {path} is an asset. Assets not supported.");
            return false;
        }

        return true;
    }

    private async Task<List<string>> FetchGuidReferences(string path, List<string> errors)
    {
        try
        {
            var jcrRequest = new RestRequest($"{path}/jcr:content.json");
            var jcrResponse = await Client.ExecuteWithErrorHandling<JcqGuidesAssets>(jcrRequest);

            return jcrResponse?.AllReferences?
                .Where(r => !string.IsNullOrEmpty(r))
                .ToList() ?? [];
        }
        catch (Exception ex)
        {
            errors.Add($"Failed to process {path}: {ex.Message}");
            return [];
        }
    }

    private async Task<List<string>> ResolveReferencesToPaths(List<string> references, string parentPath, IEnumerable<string> excludeContentWithTags, List<string> errors)
    {
        var resolvedPaths = new List<string>();
        var guidsToResolve = new List<string>();

        // Step one: segregate direct paths from GUIDs
        foreach (var reference in references)
        {
            if (reference.StartsWith("GUID-"))
                guidsToResolve.Add(Path.GetFileNameWithoutExtension(reference));
            else if (reference.StartsWith("/content/dam"))
                resolvedPaths.Add(reference);
            else
                errors.Add($"Unsupported reference format: '{reference}' in {parentPath}");
        }

        // Step two: resolve paths from GUIDs
        try
        {
            var resolvedMap = await GetNodeDataFromGuids(guidsToResolve);
            foreach (var guid in guidsToResolve)
            {
                var nodeData = resolvedMap.GetValueOrDefault(guid);

                if (nodeData is null)
                {
                    errors.Add($"Failed to resolve GUID reference for {guid} (referred in {parentPath}).");
                    continue;
                }

                if (nodeData.Tags.Intersect(excludeContentWithTags).Any())
                    continue;

                resolvedPaths.Add(nodeData.Path);
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Failed to resolve GUID references for {parentPath}: {ex.Message}");
        }

        return resolvedPaths;
    }

    private class SearchContext
    {
        public List<string> Results { get; } = [];
        public List<string> Errors { get; } = [];
        public HashSet<string> Visited { get; } = [];
    }

    public static bool IsDita(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return false;

        var ditaSigns = new List<string>
        {
            "<!DOCTYPE topic",
            "<!DOCTYPE map",
            "<!DOCTYPE concept",
            "<!DOCTYPE task",
            "DITAArchVersion",
        };

        foreach (var signs in ditaSigns)
        {
            if (content.Contains(signs))
                return true;
        }

        return false;
    }

    public async Task<Dictionary<string, DitaNodeDataDto>> GetNodeDataFromGuids(IEnumerable<string> guids)
    {
        var chunkSize = 25;
        var results = new Dictionary<string, DitaNodeDataDto>();

        var distinctGuids = guids
            .Where(g => !string.IsNullOrWhiteSpace(g))
            .Distinct()
            .ToList();

        if (distinctGuids.Count == 0)
            return results;

        foreach (var batch in distinctGuids.Chunk(chunkSize))
        {
            var request = new RestRequest("/bin/querybuilder.json")
                .AddQueryParameter("path", "/content/dam")
                .AddQueryParameter("type", "dam:Asset")
                .AddQueryParameter("p.limit", chunkSize)
                .AddQueryParameter("p.guessTotal", "true")
                .AddQueryParameter("p.hits", "selective")
                .AddQueryParameter("p.properties", "jcr:path jcr:content/fmUuid jcr:content/metadata/cq:tags")    // important: this limits what we receive
                .AddQueryParameter("property", "jcr:content/fmUuid")
                .AddQueryParameter("property.or", "true");

            var index = 1;
            foreach (var guid in batch)
            {
                request.AddQueryParameter($"property.{index}_value", guid);
                index++;
            }

            var response = await Client.ExecuteWithErrorHandling<GetPathFromGuidsQueryBuilderResponseDto>(request);

            foreach (var hit in response.Hits)
            {
                if (!string.IsNullOrEmpty(hit.Path))
                    results[hit.Content.FmUuid] = new(hit.Path, hit.Content.Metadata.Tags);
            }
        }

        return results;
    }
}
