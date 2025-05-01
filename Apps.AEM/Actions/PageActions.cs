using Apps.AEM.Models.Requests;
using Apps.AEM.Models.Responses;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.AEM.Actions;

[ActionList]
public class PageActions(InvocationContext invocationContext) : Invocable(invocationContext)
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
}