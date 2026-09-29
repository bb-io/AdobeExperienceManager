using Apps.AEM.Handlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.AEM.Models.Requests;

public class SearchContentQueryBuilderRequest
{
    [Display("Root path", Description = "The path under which content is searched.")]
    public string RootPath { get; set; } = string.Empty;

    [Display("Include root content", Description = "Include the root item when it matches the search criteria. Defaults to true.")]
    public bool? IncludeRoot { get; set; }

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

    [Display("Name pattern", Description = "Match the page name or filename, not its title or full path. Supports * (zero or more characters), ? (one character), and [abc] (one listed character). Example: about-* or *.dita.")]
    public string? NamePattern { get; set; }

    [Display("Property name", Description = "Relative property path on the matching item, for example jcr:content/jcr:title for a page. Requires Property value.")]
    public string? PropertyName { get; set; }

    [Display("Property value", Description = "Exact value to match for Property name. Both inputs must be supplied together. Wildcards are treated literally.")]
    public string? PropertyValue { get; set; }

    [Display("Exclude path", Description = "AEM filters full paths server-side using this regular expression. Example: .*/archive(/.*)? excludes archive nodes and their descendants. Exclusion cannot use a search index; narrow the root path and other filters for better performance. Max items limits returned results, not the number of nodes AEM may examine.")]
    public string? ExcludePath { get; set; }

    [Display("Created or modified after", Description = "Optional exclusive lower date bound. No lower bound is applied when omitted. Dates without a time zone are treated as UTC.")]
    public DateTime? StartDate { get; set; }

    [Display("Created or modified before", Description = "Optional exclusive upper date bound. No upper bound is applied when omitted. Dates without a time zone are treated as UTC.")]
    public DateTime? EndDate { get; set; }

    [Display("Max items to return", Description = "How many items could be returned at once. By default it's 100.")]
    public int? MaxItems { get; set; }
}
