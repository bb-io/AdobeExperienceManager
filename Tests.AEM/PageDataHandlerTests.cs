using Apps.AEM.Handlers;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Tests.AEM.Base;

namespace Tests.AEM;

public class PageDataHandlerTests : BaseDataHandlerTests
{
    protected override IAsyncDataSourceItemHandler DataHandler => new PageDataHandler(InvocationContext);

    protected override string SearchString => "Clear";
}
