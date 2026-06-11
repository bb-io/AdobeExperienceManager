using Apps.AEM.Handlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.AEM.Models.Requests;

public class ChangeFieldTagsRequest
{
    [Display("Content path", Description = "The content fragment path. Must start with /content/dam.")]
    [DataSource(typeof(ContentFragmentDataHandler))]
    public string ContentId { get; set; } = string.Empty;

    [Display("Field name", Description = "Name of the content fragment field with type tag.")]
    public string FieldName { get; set; } = string.Empty;

    [Display("Tags to add")]
    [DataSource(typeof(TagDataHandler))]
    public IEnumerable<string>? AddTags { get; set; }

    [Display("Tags to remove")]
    [DataSource(typeof(TagDataHandler))]
    public IEnumerable<string>? RemoveTags { get; set; }
}
