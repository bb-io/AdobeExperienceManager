using Apps.AEM.Constants;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.AEM.Handlers;

public class ContentTypesDataHandler : IStaticDataSourceItemHandler
{
    public IEnumerable<DataSourceItem> GetData() =>
    [
        new(ContentTypes.Page, "Pages and experience fragments"),
        new(ContentTypes.Asset, "Assets, content fragments and Dita files"),
        new(ContentTypes.File, "Raw files"),
    ];
}
