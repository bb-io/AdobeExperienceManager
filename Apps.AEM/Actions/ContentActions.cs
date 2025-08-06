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
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Blueprints;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Filters.Transformations;
using Blackbird.Filters.Xliff.Xliff2;
using Newtonsoft.Json;
using RestSharp;

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
            StartDate = input.StartDate ?? actionTime.AddDays(-31),
            EndDate = input.EndDate ?? actionTime,
            Tags = input.Tags,
            Keyword = input.Keyword,
            ContentType = input.ContentType,
            Events = input.Events,
        });

        var contentResults = await Client.Paginate<ContentResponse>(searchRequest);

        return new(contentResults);
    }

    [Action("Download content", Description = "Download content as interoperable HTML or the original JSON.")]
    [BlueprintActionDefinition(BlueprintAction.DownloadContent)]
    public async Task<DownloadContentResponse> DownloadContent(
        [ActionParameter] DownloadContentRequest input)
    {
        var request = new RestRequest("/content/services/bb-aem-connector/content-exporter.json")
            .AddQueryParameter("contentPath", input.ContentId);

        var response = await Client.ExecuteWithErrorHandling(request);
        if (response.Content == null)
        {
            throw new PluginApplicationException("Failed to retrieve content. Response content is null.");
        }

        var referenceEntities = input.IncludeReferenceContent == true
            ? await GetReferenceEntitiesAsync(response.Content)
            : [];

        var filename = ContentPathToFilenameConverter.PathToFilename(input.ContentId);

        switch (input.FileFormat ?? "text/html")
        {
            case "original":
                var jsonString = OriginalToJsonConverter.ConvertToJson(
                    response.Content,
                    input.ContentId,
                    referenceEntities,
                    input.IncludeReferenceContent == true);

                var jsonMemoryStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonString)) { Position = 0 };

                return new(await fileManagementClient.UploadAsync(jsonMemoryStream, "application/json", $"{filename}.json"));

            case "text/html":
                var htmlString = JsonToHtmlConverter.ConvertToHtml(
                    response.Content,
                    input.ContentId,
                    referenceEntities);

                var memoryStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(htmlString)) { Position = 0 };

                return new(await fileManagementClient.UploadAsync(memoryStream, "text/html", $"{filename}.html"));

            default:
                throw new PluginMisconfigurationException($"Unsupported file format: {input.FileFormat}");
        }
    }

    [Action("Upload content", Description = "Update content at the specified path, or create it if it does not exist. Accepts a translated file (interoperable HTML or XLIFF) and the original JSON file as input.")]
    [BlueprintActionDefinition(BlueprintAction.UploadContent)]
    public async Task<IEnumerable<UploadContentResponse>> UploadContent([ActionParameter] UploadContentRequest input)
    {
        if (!string.IsNullOrWhiteSpace(input.ContentId) && input.SkipUpdatingReferences != true)
        {
            throw new PluginMisconfigurationException("'ContentId' can only be set with 'SkipUpdatingReferences' being set to true, as path overwrite only impacts a main (root) content.");
        }

        var fileStream = await fileManagementClient.DownloadAsync(input.Content);
        var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        var inputString = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());

        if (Xliff2Serializer.IsXliff2(inputString))
        {
            inputString = Transformation.Parse(inputString, input.Content.Name).Target().Serialize()
                ?? throw new PluginMisconfigurationException("XLIFF did not contain any files");
        }

        var entities = OriginalJsonValidator.IsJson(inputString)
            ? JsonToOriginalConverter.ConvertToEntities(inputString)
            : HtmlToJsonConverter.ConvertToJson(inputString);

        var uploadResults = new List<UploadContentResponse>();

        foreach (var entity in entities)
        {
            if (entity.ReferenceContent && input.SkipUpdatingReferences == true)
            {
                continue;
            }

            try
            {
                // allow overwriting source path from the action input
                // so that target con can be uploaded to a very different path from source
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

                var request = new RestRequest("/content/services/bb-aem-connector/content-importer", Method.Post)
                    .AddHeader("Content-Type", "application/json")
                    .AddHeader("Accept", "application/json")
                    .AddStringBody(jsonString, DataFormat.Json);

                var uploadResult = await Client.ExecuteWithErrorHandling<UploadContentResponse>(request);
                if (string.IsNullOrEmpty(uploadResult.Message))
                {
                    throw new PluginApplicationException($"Failed to upload content. No message returned from server.");
                }

                uploadResults.Add(new()
                {
                    ContentId = targetPath,
                    Message = uploadResult.Message.Replace(
                        "Content imported successfully",
                        "Content uploaded successfully"),
                });
            }
            catch (Exception ex)
            {
                if (entity.ReferenceContent && input.IgnoreReferenceContentErrors == true)
                {
                    uploadResults.Add(new()
                    {
                        ContentId = entity.SourcePath,
                        Message = $"Ignored error during reference content upload: {ex.Message}",
                    });
                    continue;
                }

                throw;
            }
        }

        return uploadResults;
    }

    private static string ModifyPath(string path, string sourceLanguage, string targetLanguage)
    {
        if (string.IsNullOrWhiteSpace(path) || string.IsNullOrEmpty(sourceLanguage) || string.IsNullOrEmpty(targetLanguage))
        {
            throw new PluginApplicationException("Main (root) rath, source language, and target language must be provided.");
        }
        
        return path.Replace(sourceLanguage, targetLanguage);
    }

    private async Task<IEnumerable<ReferenceEntity>> GetReferenceEntitiesAsync(string content)
    {
        var processedPaths = new HashSet<string>();
        return await ProcessReferencesRecursivelyAsync(content, processedPaths, 0);
    }

    private async Task<IEnumerable<ReferenceEntity>> ProcessReferencesRecursivelyAsync(
        string content,
        ISet<string> processedPaths,
        int depth,
        int maxDepth = 100)
    {
        var referenceEntities = new List<ReferenceEntity>();

        if (depth > maxDepth || string.IsNullOrEmpty(content))
            return referenceEntities;

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
                referenceEntities.Add(new ReferenceEntity(
                    reference.ReferencePath,
                    referenceResponse.Content,
                    reference.PropertyName,
                    reference.PropertyPath));

                referenceEntities.AddRange(await ProcessReferencesRecursivelyAsync(
                    referenceResponse.Content,
                    processedPaths,
                    depth + 1,
                    maxDepth));
            }
        }

        return referenceEntities;
    }
}