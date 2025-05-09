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
    public async Task<FileResponse> GetPageAsHtmlAsync([ActionParameter] PageRequest pageRequest)
    {
        var request = new RestRequest("/content/services/bb-aem-connector/page-exporter.json")
            .AddQueryParameter("pagePath", pageRequest.PagePath);
        
        var response = await Client.ExecuteWithErrorHandling(request);
        var htmlString = JsonToHtmlConverter.ConvertToHtml(response.Content!, pageRequest.PagePath);

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
        var jsonContent = HtmlToJsonConverter.ConvertToJson(htmlString);

        var jsonString = JsonConvert.SerializeObject(new
        {
            sourcePath,
            targetPath = pageRequest.TargetPagePath,
            targetContent = jsonContent
        }, Formatting.None);

        var request = new RestRequest("/content/services/bb-aem-connector/page-importer.json", Method.Post);
        request.AddHeader("Content-Type", "application/json");
        request.AddHeader("Accept", "application/json");

        request.AddStringBody(jsonString, DataFormat.Json);

        var result = await Client.ExecuteWithErrorHandling<UpdatePageFromHtmlResponse>(request);
        if(string.IsNullOrEmpty(result.Message))
        {
            throw new PluginApplicationException("Update failed. No message returned from server.");
        }

        return result;
    }

    private RestRequest BuildPageSearchRequest(SearchPagesRequest searchCriteria)
    {
        var request = new RestRequest("/content/services/bb-aem-connector/pages/events.json");
        
        if(searchCriteria.RootPath != null)
        {
            request.AddQueryParameter("rootPath", searchCriteria.RootPath);
        }

        bool hasStartDate = searchCriteria.CreatedAfter.HasValue || searchCriteria.ModifiedAfter.HasValue;
        bool hasEndDate = searchCriteria.CreatedBefore.HasValue || searchCriteria.ModifiedBefore.HasValue;

        if(hasStartDate && hasEndDate)
        {
            throw new PluginMisconfigurationException("You can only set created date or modified date, not both.");
        }

        if(hasEndDate)
        {
            request.AddQueryParameter("events", "modified");
        }

        if(hasStartDate)
        {
            request.AddQueryParameter("events", "created");
        }

        if(searchCriteria.CreatedAfter.HasValue)
        {
            request.AddQueryParameter("startDate", searchCriteria.CreatedAfter.Value.ToString("yyyy-MM-ddTHH:mm:ssZ"));
        }

        if(searchCriteria.CreatedBefore.HasValue)
        {
            request.AddQueryParameter("endDate", searchCriteria.CreatedBefore.Value.ToString("yyyy-MM-ddTHH:mm:ssZ"));
        }

        if(searchCriteria.ModifiedAfter.HasValue)
        {
            request.AddQueryParameter("startDate", searchCriteria.ModifiedAfter.Value.ToString("yyyy-MM-ddTHH:mm:ssZ"));
        }

        if(searchCriteria.ModifiedBefore.HasValue)
        {
            request.AddQueryParameter("endDate", searchCriteria.ModifiedBefore.Value.ToString("yyyy-MM-ddTHH:mm:ssZ"));
        }
        
        return request;
    }
}