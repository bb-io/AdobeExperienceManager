using Apps.AEM.Models.ApiPayloads;
using Apps.AEM.Models.Dtos;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace Apps.AEM.Handlers;

public class ContentFragmentDataHandler(InvocationContext invocationContext) : Invocable(invocationContext), IAsyncDataSourceItemHandler
{
    private const string RootPath = "/content/dam";
    private const int MaxItems = 20;
    private const string FragmentsSearchEndpoint = "/adobe/sites/cf/fragments/search";

    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context, CancellationToken cancellationToken)
    {
        var searchString = context.SearchString?.Trim();
        var items = new List<DataSourceItem>();
        string? cursor = null;

        do
        {
            var request = new RestRequest(FragmentsSearchEndpoint)
                .AddQueryParameter("limit", MaxItems)
                .AddQueryParameter("projection", "summary")
                .AddQueryParameter("query", BuildSearchQuery(searchString));

            if (!string.IsNullOrWhiteSpace(cursor))
            {
                request.AddQueryParameter("cursor", cursor);
            }

            var response = await Client.ExecuteWithErrorHandling<CursorPaginationDto<ContentFragmentDto>>(request);

            var filteredItems = response.Items
                .Where(fragment => MatchesTitle(fragment, searchString))
                .Select(fragment => new DataSourceItem(fragment.Path, GetFragmentModelShortenedName(fragment)));

            items.AddRange(filteredItems);
            cursor = response.Cursor;
        }
        while (!string.IsNullOrWhiteSpace(cursor) && items.Count < MaxItems);

        return items
            .DistinctBy(item => item.Value)
            .Take(MaxItems);
    }

    private static string BuildSearchQuery(string? searchString)
    {
        var filter = new JObject
        {
            ["path"] = RootPath
        };

        if (!string.IsNullOrWhiteSpace(searchString))
        {
            // The dedicated search API does not define a title-specific filter in the spec.
            // Use full-text search to reduce the candidate set, then enforce title-only matching locally.
            filter["fullText"] = new JObject
            {
                ["text"] = searchString,
                ["queryMode"] = "AS_IS"
            };
        }

        var query = new JObject
        {
            ["filter"] = filter,
            ["sort"] = new JArray
            {
                new JObject
                {
                    ["on"] = "modified",
                    ["order"] = "DESC"
                }
            }
        };

        return query.ToString(Formatting.None);
    }

    private static bool MatchesTitle(ContentFragmentDto fragment, string? searchString)
    {
        return string.IsNullOrWhiteSpace(searchString)
            || (!string.IsNullOrWhiteSpace(fragment.Title)
                && fragment.Title.Contains(searchString, StringComparison.OrdinalIgnoreCase));
    }

    private static string GetFragmentModelShortenedName(ContentFragmentDto fragment)
    {
        if (fragment.Model?.Name is null)
            return fragment.Title;

        if (fragment.Model?.Name.Length <= 15)
            return fragment.Model.Name + ": " + fragment.Title;

        return fragment.Model?.Name.Substring(0, 15) + "…: " + fragment.Title ?? fragment.Title;
    }
}
