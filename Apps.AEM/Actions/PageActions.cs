using Apps.AEM.Models.Requests;
using Apps.AEM.Models.Responses;
using Apps.AEM.Utils.Converters;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using RestSharp;

namespace Apps.AEM.Actions;

[ActionList]
public class PageActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient) : Invocable(invocationContext)
{
    [Action("Search pages", Description = "Search for pages based on provided criteria.")]
    public async Task<SearchPagesResponse> SearchPagesAsync([ActionParameter] SearchPagesRequest searchPagesRequest)
    {
        var request = new RestRequest("/content/services/bb-aem-connector/pages/events.json");
        if(searchPagesRequest.RootPath != null)
        {
            request.AddQueryParameter("rootPath", searchPagesRequest.RootPath);
        }

        if(searchPagesRequest.StartDate.HasValue)
        {
            request.AddQueryParameter("startDate", searchPagesRequest.StartDate.Value.ToString("yyyy-MM-ddTHH:mm:ssZ"));
        }

        if(searchPagesRequest.EndDate.HasValue)
        {
            request.AddQueryParameter("endDate", searchPagesRequest.EndDate.Value.ToString("yyyy-MM-ddTHH:mm:ssZ"));
        }

        if(searchPagesRequest.Events != null && searchPagesRequest.Events.Any())
        {
            request.AddQueryParameter("events", string.Join(",", searchPagesRequest.Events!));
        }

        var pages = await Client.Paginate<PageResponse>(request);
        return new(pages);
    }

    [Action("Get page as HTML", Description = "Get the HTML content of a page.")]
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
}