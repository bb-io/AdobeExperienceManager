using Apps.AEM.Constants;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.AEM.Handlers;

public class ContentFragmentStatusesDataHandler : IStaticDataSourceItemHandler
{
    public IEnumerable<DataSourceItem> GetData() =>
    [
        new(ContentFragmentStatuses.New, "New"),
        new(ContentFragmentStatuses.Draft, "Draft"),
        new(ContentFragmentStatuses.Published, "Published"),
        new(ContentFragmentStatuses.Modified, "Modified"),
        new(ContentFragmentStatuses.Unpublished, "Unpublished"),
    ];
}
