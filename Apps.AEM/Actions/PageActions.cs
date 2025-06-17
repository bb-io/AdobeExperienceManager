using Apps.AEM.Models.Entities;
using Apps.AEM.Models.Requests;
using Apps.AEM.Models.Responses;
using Apps.AEM.Utils.Converters;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Newtonsoft.Json;
using RestSharp;

namespace Apps.AEM.Actions;

[ActionList]
public class PageActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient) : Invocable(invocationContext)
{
    [Action("Search content", Description = "Search for content based on provided criteria.")]
    public async Task<SearchPagesResponse> SearchPagesAsync([ActionParameter] SearchPagesRequest searchPagesRequest)
    {
        var searchRequest = BuildPageSearchRequest(searchPagesRequest);
        var pageResults = await Client.Paginate<PageResponse>(searchRequest);
        return new(pageResults);
    }

    [Action("Download content", Description = "Download content as HTML.")]
    public async Task<FileResponse> GetPageAsHtmlAsync([ActionParameter] PageRequest pageRequest,
        [ActionParameter] DownloadContentRequest downloadContentRequest)
    {
        var request = new RestRequest("/content/services/bb-aem-connector/content-exporter.json")
            .AddQueryParameter("contentPath", pageRequest.PagePath);

        var response = await Client.ExecuteWithErrorHandling(request);
        var referenceEntities = downloadContentRequest.IncludeReferenceContnent == true
            ? await GetReferenceEntitiesAsync(response.Content!)
            : new List<ReferenceEntity>();

        var htmlString = JsonToHtmlConverter.ConvertToHtml(response.Content!, pageRequest.PagePath, referenceEntities);
        var memoryStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(htmlString));
        memoryStream.Position = 0;

        var title = JsonToHtmlConverter.ExtractTitle(response.Content!);
        var fileReference = await fileManagementClient.UploadAsync(memoryStream, "text/html", $"{title}.html");

        return new(fileReference);
    }

    [Action("Upload content", Description = "Upload content from HTML.")]
    public async Task<UpdatePageFromHtmlResponse> UpdatePageFromHtmlAsync([ActionParameter] UpdatePageFromHtmlRequest pageRequest)
    {
        var fileStream = await fileManagementClient.DownloadAsync(pageRequest.File);
        var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        var htmlString = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());
        var sourcePath = HtmlToJsonConverter.ExtractSourcePath(htmlString);
        var entities = HtmlToJsonConverter.ConvertToJson(htmlString);
        var lastResult = new UpdatePageFromHtmlResponse();
        
        foreach (var entity in entities)
        {
            try
            {
                if (string.IsNullOrEmpty(entity.SourcePath))
                {
                    entity.SourcePath = sourcePath;
                }

                var referenceTargetPath = ModifyPath(entity.SourcePath, pageRequest.SourceLanguage, pageRequest.TargetLanguage);
                if (entity.References != null && !string.IsNullOrEmpty(pageRequest.SourceLanguage) && !string.IsNullOrEmpty(pageRequest.TargetLanguage))
                {
                    ModifyReferencePaths(entity.References, pageRequest.SourceLanguage, pageRequest.TargetLanguage);
                }
                
                var jsonString = JsonConvert.SerializeObject(new
                {
                    sourcePath = entity.SourcePath,
                    targetPath = entity.ReferenceContent ? referenceTargetPath : pageRequest.TargetPagePath,
                    targetContent = entity.TargetContent,
                    references = entity.References,
                }, Formatting.Indented);

                var request = new RestRequest("/content/services/bb-aem-connector/content-importer", Method.Post)
                    .AddHeader("Content-Type", "application/json")
                    .AddHeader("Accept", "application/json")
                    .AddStringBody(jsonString, DataFormat.Json);

                lastResult = await Client.ExecuteWithErrorHandling<UpdatePageFromHtmlResponse>(request);
                if (string.IsNullOrEmpty(lastResult.Message))
                {
                    throw new PluginApplicationException("Update failed. No message returned from server.");
                }
            }
            catch (Exception ex)
            {
                if (entity.ReferenceContent && pageRequest.IgnoreReferenceContentErrors == true)
                {
                    lastResult = new UpdatePageFromHtmlResponse
                    {
                        Message = $"Ignored error in reference content: {ex.Message}"
                    };
                    continue;
                }

                throw;
            }
        }

        return lastResult;
    }

    private string ModifyPath(string path, string? sourceLanguage, string? targetLanguage)
    {
        if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(sourceLanguage) || string.IsNullOrEmpty(targetLanguage))
        {
            return path;
        }
        
        return path.Replace($"/{sourceLanguage}", $"/{targetLanguage}");
    }
    
    private void ModifyReferencePaths(IEnumerable<ReferenceEntity> references, string sourceLanguage, string targetLanguage)
    {
        if (references == null || string.IsNullOrEmpty(sourceLanguage) || string.IsNullOrEmpty(targetLanguage))
        {
            return;
        }
        
        foreach (var reference in references)
        {
            if (reference != null && !string.IsNullOrEmpty(reference.ReferencePath))
            {
                reference.ReferencePath = ModifyPath(reference.ReferencePath, sourceLanguage, targetLanguage);
            }
        }
    }

    private RestRequest BuildPageSearchRequest(SearchPagesRequest searchCriteria)
    {
        var request = new RestRequest("/content/services/bb-aem-connector/pages/events.json");

        if (searchCriteria.RootPath != null)
        {
            request.AddQueryParameter("rootPath", searchCriteria.RootPath);
        }

        bool hasStartDate = searchCriteria.CreatedAfter.HasValue || searchCriteria.ModifiedAfter.HasValue;
        bool hasEndDate = searchCriteria.CreatedBefore.HasValue || searchCriteria.ModifiedBefore.HasValue;

        if (hasStartDate && hasEndDate)
        {
            throw new PluginMisconfigurationException("You can only set created date or modified date, not both.");
        }

        if (hasEndDate)
        {
            request.AddQueryParameter("events", "modified");
        }

        if (hasStartDate)
        {
            request.AddQueryParameter("events", "created");
        }

        if (searchCriteria.CreatedAfter.HasValue)
        {
            request.AddQueryParameter("startDate", searchCriteria.CreatedAfter.Value.ToString("yyyy-MM-ddTHH:mm:ssZ"));
        }

        if (searchCriteria.CreatedBefore.HasValue)
        {
            request.AddQueryParameter("endDate", searchCriteria.CreatedBefore.Value.ToString("yyyy-MM-ddTHH:mm:ssZ"));
        }

        if (searchCriteria.ModifiedAfter.HasValue)
        {
            request.AddQueryParameter("startDate", searchCriteria.ModifiedAfter.Value.ToString("yyyy-MM-ddTHH:mm:ssZ"));
        }

        if (searchCriteria.ModifiedBefore.HasValue)
        {
            request.AddQueryParameter("endDate", searchCriteria.ModifiedBefore.Value.ToString("yyyy-MM-ddTHH:mm:ssZ"));
        }

        return request;
    }

    private async Task<List<ReferenceEntity>> GetReferenceEntitiesAsync(string content)
    {
        var referenceEntities = new List<ReferenceEntity>();
        var processedPaths = new HashSet<string>();

        await ProcessReferencesRecursivelyAsync(content, referenceEntities, processedPaths, 0);
        return referenceEntities;
    }

    private async Task ProcessReferencesRecursivelyAsync(
        string content,
        List<ReferenceEntity> referenceEntities,
        HashSet<string> processedPaths,
        int depth,
        int maxDepth = 100)
    {
        if (depth > maxDepth || string.IsNullOrEmpty(content))
            return;

        var references = JsonToHtmlConverter.ExtractReferences(content);
        foreach (var reference in references)
        {
            if (string.IsNullOrEmpty(reference.ReferencePath) || processedPaths.Contains(reference.ReferencePath))
            {
                continue;
            }

            processedPaths.Add(reference.ReferencePath);
            var referenceRequest = new RestRequest("/content/services/bb-aem-connector/content-exporter.json")
                .AddQueryParameter("contentPath", reference.ReferencePath);

            var referenceResponse = await Client.ExecuteWithErrorHandling(referenceRequest);
            if (referenceResponse.IsSuccessful && !string.IsNullOrEmpty(referenceResponse.Content))
            {
                var referenceEntity = new ReferenceEntity(
                    reference.ReferencePath,
                    referenceResponse.Content!,
                    reference.PropertyName,
                    reference.PropertyPath);

                referenceEntities.Add(referenceEntity);
                await ProcessReferencesRecursivelyAsync(referenceResponse.Content!, referenceEntities, processedPaths, depth + 1, maxDepth);
            }
        }
    }
}