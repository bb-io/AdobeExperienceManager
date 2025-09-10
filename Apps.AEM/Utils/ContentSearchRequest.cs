using Apps.AEM.Constants;
using Apps.AEM.Handlers;
using Apps.AEM.Models.Dtos;
using RestSharp;

namespace Apps.AEM.Utils;

public static class ContentSearch
{
    public const string SearchDateFormat = "yyyy-MM-ddTHH:mm:ssZ";

    public static RestRequest BuildRequest(SearchRequestDto searchParams)
    {
        var request = new RestRequest("/content/services/bb-aem-connector/content/events.json");

        // Dates
        request.AddQueryParameter("startDate", searchParams.StartDate.ToString(SearchDateFormat));
        request.AddQueryParameter("endDate", searchParams.EndDate.ToString(SearchDateFormat));

        // Root path filter
        if (searchParams.RootPath != null)
        {
            request.AddQueryParameter("rootPath", searchParams.RootPath);
        }

        // Content type filter
        request.AddQueryParameter("type", searchParams.ContentType ?? ContentTypes.Page);

        // Event types
        var events = searchParams.Events
            ?? new EventsDataHandler().GetData().Select(x => x.Value);
        foreach (var eventType in events)
        {
            request.AddQueryParameter("events", eventType);
        }

        // Tags
        foreach (var tag in searchParams.Tags ?? [])
        {
            request.AddQueryParameter("tags", tag);
        }

        // Keyword
        if (!string.IsNullOrEmpty(searchParams.Keyword))
        {
            request.AddQueryParameter("keyword", searchParams.Keyword);
        }

        // Offset
        if (searchParams.Offset.HasValue)
        {
            request.AddQueryParameter("offset", searchParams.Offset ?? 0);
        }

        // Limit
        if (searchParams.Limit.HasValue)
        {
            request.AddQueryParameter("limit", searchParams.Limit ?? -1);
        }

        return request;
    }
}
