using Blackbird.Applications.Sdk.Common;

namespace Apps.AEM.Models.Requests;

public class SearchPagesRequest
{
    [Display("Root path", Description = "The path under which pages are searched.")]
    public string? RootPath { get; set; }

    [Display("Start date", Description = "Start date for filtering events (in ISO8601 format YYYY-MM-DDTHH:mm:ss.SSSZ or partial representations, like YYYY-MM-DD)")]
    public DateTime? StartDate { get; set; }

    [Display("End date", Description = "End date for filtering events (in ISO8601 format YYYY-MM-DDTHH:mm:ss.SSSZ or partial representations, like YYYY-MM-DD).")]
    public DateTime? EndDate { get; set; }
}