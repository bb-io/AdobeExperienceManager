using Apps.AEM.Constants;
using Apps.AEM.Handlers;
using Apps.AEM.Models.Dtos;
using Apps.AEM.Models.Requests;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Newtonsoft.Json.Linq;
using RestSharp;
using System.Globalization;

namespace Apps.AEM.Utils;

public static class ContentSearch
{
    public const string SearchDateFormat = "yyyy-MM-ddTHH:mm:ssZ";

    public static DateTime? ParseQueryBuilderDate(JToken? token)
    {
        if (token == null || token.Type == JTokenType.Null)
            return null;

        if (token.Type == JTokenType.Date)
            return token.Value<DateTime>().ToUniversalTime();

        var value = token.Value<string>();
        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date)
            || DateTimeOffset.TryParseExact(value, ["ddd MMM dd yyyy HH:mm:ss 'GMT'zzz", "ddd MMM d yyyy HH:mm:ss 'GMT'zzz"],
                CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            return date.UtcDateTime;

        throw new PluginApplicationException($"AEM QueryBuilder returned an invalid date: {value}");
    }

    public static RestRequest BuildQueryBuilderRequest(SearchContentQueryBuilderRequest input)
    {
        if (input.MaxItems <= 0)
            throw new PluginMisconfigurationException("Max items to return must be greater than zero.");

        var propertyName = input.PropertyName?.Trim();
        if (!string.IsNullOrEmpty(propertyName) != (input.PropertyValue != null))
            throw new PluginMisconfigurationException("Property name and property value must be supplied together.");

        // Blackbird dates without an explicit time zone represent UTC; local dates must be converted.
        var start = input.StartDate is DateTime startDate
            ? (startDate.Kind == DateTimeKind.Local ? startDate.ToUniversalTime() : DateTime.SpecifyKind(startDate, DateTimeKind.Utc))
            : (DateTime?)null;
        var end = input.EndDate is DateTime endDate
            ? (endDate.Kind == DateTimeKind.Local ? endDate.ToUniversalTime() : DateTime.SpecifyKind(endDate, DateTimeKind.Utc))
            : (DateTime?)null;

        if (start.HasValue && end.HasValue && start >= end)
            throw new PluginMisconfigurationException("The start date must be before the end date.");

        var contentType = input.ContentType ?? ContentTypes.Page;
        var titleProperties = contentType == ContentTypes.Page
            ? "jcr:content/pageTitle jcr:content/jcr:title"
            : "jcr:content/jcr:title jcr:content/fmditaTitle";
        var request = new RestRequest("/bin/querybuilder.json")
            .AddQueryParameter("path", input.RootPath?.Trim() ?? "/")
            .AddQueryParameter("path.self", (input.IncludeRoot ?? true) ? "true" : "false")
            .AddQueryParameter("type", contentType)
            .AddQueryParameter("p.hits", "selective")
            .AddQueryParameter("p.properties", $"jcr:path jcr:created {titleProperties} jcr:content/cq:lastModified jcr:content/jcr:lastModified")
            .AddQueryParameter("p.limit", input.MaxItems ?? 100)
            .AddQueryParameter("p.offset", 0)
            .AddQueryParameter("p.guessTotal", "true")
            .AddQueryParameter("p.excerpt", "false");

        if (start.HasValue || end.HasValue)
        {
            var events = input.Events?.ToHashSet() ?? [];
            var createdOnly = events.SetEquals(["created"]);
            var modifiedOnly = events.SetEquals(["modified"]);
            var modifiedProperty = contentType == ContentTypes.Page
                ? "jcr:content/cq:lastModified"
                : "jcr:content/jcr:lastModified";
            var properties = createdOnly ? new[] { "jcr:created" }
                : modifiedOnly ? new[] { modifiedProperty }
                : new[] { "jcr:created", modifiedProperty };

            request.AddQueryParameter("1_group.p.or", "true");
            for (var i = 0; i < properties.Length; i++)
            {
                var prefix = $"1_group.{i + 1}_daterange";
                request.AddQueryParameter($"{prefix}.property", properties[i]);
                if (start.HasValue)
                {
                    request.AddQueryParameter($"{prefix}.lowerBound", start.Value.ToString("O", CultureInfo.InvariantCulture));
                    request.AddQueryParameter($"{prefix}.lowerOperation", ">");
                }
                if (end.HasValue)
                {
                    request.AddQueryParameter($"{prefix}.upperBound", end.Value.ToString("O", CultureInfo.InvariantCulture));
                    request.AddQueryParameter($"{prefix}.upperOperation", "<");
                }
            }
        }

        var tags = input.Tags?.ToArray() ?? [];
        if (tags.Length > 0)
        {
            request.AddQueryParameter("property", contentType == ContentTypes.Asset
                ? "jcr:content/metadata/cq:tags" : "jcr:content/cq:tags");
            request.AddQueryParameter("property.and", "false");
            for (var i = 0; i < tags.Length; i++)
                request.AddQueryParameter($"property.{i + 1}_value", tags[i]);
        }

        if (!string.IsNullOrEmpty(input.Keyword))
            request.AddQueryParameter("fulltext", input.Keyword);

        if (!string.IsNullOrWhiteSpace(input.NamePattern))
            request.AddQueryParameter("nodename", input.NamePattern.Trim());

        // Keep the custom property separate from the tag predicate and date OR group.
        if (!string.IsNullOrEmpty(propertyName))
        {
            request.AddQueryParameter("2_property", propertyName);
            request.AddQueryParameter("2_property.operation", "equals");
            request.AddQueryParameter("2_property.value", input.PropertyValue!);
        }

        if (!string.IsNullOrWhiteSpace(input.ExcludePath))
            request.AddQueryParameter("excludepaths", input.ExcludePath);

        return request;
    }

    public static RestRequest BuildRequest(SearchRequestDto searchParams)
    {
        var request = new RestRequest("/content/services/bb-aem-connector/content/events.json");

        // Dates
        if (searchParams.StartDate is DateTime startDate)
            request.AddQueryParameter("startDate", startDate.ToString(SearchDateFormat));

        if (searchParams.EndDate is DateTime endDate)
            request.AddQueryParameter("endDate", endDate.ToString(SearchDateFormat));

        // Root path filter
        if (searchParams.RootPath is not null)
            request.AddQueryParameter("rootPath", searchParams.RootPath);

        // Content type filter
        request.AddQueryParameter("type", searchParams.ContentType ?? ContentTypes.Page);

        // Event types
        var events = searchParams.Events
            ?? new EventsDataHandler().GetData().Select(x => x.Value);

        foreach (var eventType in events)
            request.AddQueryParameter("events", eventType);

        // Tags
        foreach (var tag in searchParams.Tags ?? [])
            request.AddQueryParameter("tags", tag);

        // Keyword
        if (!string.IsNullOrEmpty(searchParams.Keyword))
            request.AddQueryParameter("keyword", searchParams.Keyword);

        // Offset
        if (searchParams.Offset.HasValue)
            request.AddQueryParameter("offset", searchParams.Offset ?? 0);

        // Limit
        if (searchParams.Limit.HasValue)
            request.AddQueryParameter("limit", searchParams.Limit ?? -1);

        return request;
    }
}
