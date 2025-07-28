using Apps.AEM.Constants;
using Apps.AEM.Models.Responses;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.AEM.Handlers;

public class PageDataHandler(InvocationContext invocationContext) : Invocable(invocationContext), IAsyncDataSourceItemHandler
{
    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context, CancellationToken cancellationToken)
    {
        var request = new RestRequest("/content/services/bb-aem-connector/pages/events.json");
        request.AddParameter("type", DataTypes.Page);

        if (!string.IsNullOrEmpty(context.SearchString))
        {
            request.AddQueryParameter("keyword", context.SearchString);
        }

        var pages = await Client.Paginate<PageResponse>(request, 100);

        return pages
            .Where(x => context.SearchString == null || x.Title.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase))
            .Select(x => new DataSourceItem(x.Path, x.Title));
    }
}