using Apps.AEM.Handlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.AEM.Events.Models;

public class OnContentFragmentTagAddedRequest
{
    [Display("Tags", Description = "Fire when any of the listed tags is newly added to a content fragment.")]
    [DataSource(typeof(TagDataHandler))]
    public IEnumerable<string> Tags { get; set; } = [];

    [Display("Root path", Description = "The content fragment root path to monitor. Must start with /content/dam.")]
    public string RootPath { get; set; } = "/content/dam";

    [Display("Statuses (all by default)", Description = "Only content fragments with one of these statuses will be monitored. Leave empty to include all statuses.")]
    [StaticDataSource(typeof(ContentFragmentStatusesDataHandler))]
    public IEnumerable<string>? Statuses { get; set; }
}
