using Apps.AEM.Handlers;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tests.AEM.Base;

namespace Tests.AEM;

[TestClass]
public class TagDataHandlerTests : BaseDataHandlerTests
{
    protected override IAsyncDataSourceItemHandler DataHandler => new TagDataHandler(InvocationContext);

    protected override string SearchString => "Translation";
}
