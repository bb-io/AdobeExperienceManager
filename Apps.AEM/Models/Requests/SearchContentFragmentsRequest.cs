using Apps.AEM.Handlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.AEM.Models.Requests;

public class SearchContentFragmentsRequest
{
    [Display("Path begins with", Description = "The path under /content/dam where content fragments are searched.")]
    public string RootPath { get; set; } = "/content/dam";

    [Display("Tags", Description = "Find content fragments that have at least one of the listed tags.")]
    [DataSource(typeof(TagDataHandler))]
    public IEnumerable<string>? Tags { get; set; }

    [Display("Max items to return", Description = "How many content fragments to return at once. Defaults to 100.")]
    public int? MaxItems { get; set; }
}
