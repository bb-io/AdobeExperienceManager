using Apps.AEM.Handlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;

namespace Apps.AEM.Events.Models;

public class OnPagesCreatedOrUpdatedRequest
{
    [Display("Root path")]
    public string? RootPath { get; set; }

    [Display("Root path includes")]
    public IEnumerable<string>? RootPathIncludes { get; set; }

    [Display("Tags", Description = "Find pages that have at least one of the listed tags.")]
    public IEnumerable<string>? Tags { get; set; }

    [Display("Keyword", Description = "Keyword to search for content, uses the AEM's full-text search.")]
    public string? Keyword { get; set; }

    [Display("Content type", Description = "Type of content to search for, defaults to 'page'.")]
    public string? ContentType { get; set; }

    [Display("Events (all by default)")]
    [StaticDataSource(typeof(EventsDataHandler))]
    public IEnumerable<string>? Events { get; set; }
}
