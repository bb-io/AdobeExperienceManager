using Apps.AEM.Constants;
using Apps.AEM.Models.Responses;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.AEM.Handlers;

public class PageDataHandler(InvocationContext invocationContext) : Invocable(invocationContext), IAsyncDataSourceItemHandler
{
    private const int MaxItems = 25;

    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context, CancellationToken cancellationToken)
    {
        var request = new RestRequest("/content/services/bb-aem-connector/content/events.json");
        request.AddParameter("type", ContentTypes.Page);
        request.AddParameter("limit", MaxItems);

        if (!string.IsNullOrEmpty(context.SearchString))
        {
            request.AddQueryParameter("keyword", context.SearchString);
        }

        var pages = await Client.Paginate<ContentResponse>(request, 25);

        return pages
            .Where(x => context.SearchString == null || x.Title.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase))
            .Select(x => new DataSourceItem(x.ContentId, x.Title));
    }
}