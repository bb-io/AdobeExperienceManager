using Blackbird.Applications.Sdk.Common;

namespace Apps.AEM.Models.Responses;

public class GetContentFragmentResponse
{
    [Display("ID")]
    public string Id { get; set; } = string.Empty;

    [Display("Path")]
    public string Path { get; set; } = string.Empty;

    [Display("Title")]
    public string Title { get; set; } = string.Empty;

    [Display("Description")]
    public string Description { get; set; } = string.Empty;

    [Display("Created by")]
    public string? CreatedBy { get; set; }

    [Display("Created at")]
    public DateTime? CreatedAt { get; set; }

    [Display("Modified by")]
    public string? ModifiedBy { get; set; }

    [Display("Modified at")]
    public DateTime? ModifiedAt { get; set; }

    [Display("Status")]
    public string Status { get; set; } = string.Empty;

    [Display("Fields")]
    public List<ContentFragmentFieldResponse> Fields { get; set; } = [];

    [Display("Variation titles")]
    public List<string> VariationTitles { get; set; } = [];

    [Display("Tags")]
    public List<string> Tags { get; set; } = [];

    [Display("Model path")]
    public string ModelPath { get; set; } = string.Empty;
}

public class ContentFragmentFieldResponse
{
    [Display("Type")]
    public string Type { get; set; } = string.Empty;

    [Display("Name")]
    public string Name { get; set; } = string.Empty;

    [Display("Multiple")]
    public bool Multiple { get; set; }
}
