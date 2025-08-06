using Apps.AEM.Handlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.AEM.Events.Models;

public class OnContentCreatedOrUpdatedRequest
{
    [Display("Root path")]
    public string? ContentId { get; set; }

    [Display("Root path includes")]
    public IEnumerable<string>? ContentIdIncludes { get; set; }

    [Display("Tags", Description = "Find content that have at least one of the listed tags.")]
    [DataSource(typeof(TagDataHandler))]
    public IEnumerable<string>? Tags { get; set; }

    [Display("Keyword", Description = "Keyword to search for content, uses the AEM's full-text search.")]
    public string? Keyword { get; set; }

    [Display("Content type", Description = "Type of content to search for, defaults to 'page'.")]
    [StaticDataSource(typeof(ContentTypesDataHandler))]
    public string? ContentType { get; set; }

    [Display("Events (all by default)")]
    [StaticDataSource(typeof(EventsDataHandler))]
    public IEnumerable<string>? Events { get; set; }
}
