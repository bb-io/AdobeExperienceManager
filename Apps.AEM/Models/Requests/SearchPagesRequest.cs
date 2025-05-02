using Blackbird.Applications.Sdk.Common;

namespace Apps.AEM.Models.Requests;

public class SearchPagesRequest
{
    [Display("Root path", Description = "The path under which pages are searched.")]
    public string? RootPath { get; set; }

    [Display("Created after", Description = "Created after date for filtering pages")]
    public DateTime? CreatedAfter { get; set; }

    [Display("Created before", Description = "Created before date for filtering pages")]
    public DateTime? CreatedBefore { get; set; }

    [Display("Modified after", Description = "Modified after date for filtering pages")]
    public DateTime? ModifiedAfter { get; set; } 

    [Display("Modified before", Description = "Modified before date for filtering pages")]
    public DateTime? ModifiedBefore { get; set; }
}