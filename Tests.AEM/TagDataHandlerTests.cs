using Apps.AEM.Handlers;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using Tests.AEM.Base;

namespace Tests.AEM;

[TestClass]
public class TagDataHandlerTests : BaseDataHandlerTests
{
    protected override IAsyncDataSourceItemHandler CreateDataHandler(InvocationContext context) => new TagDataHandler(context);

    protected override string SearchString => "Translation";
}
