using Apps.AEM.Handlers;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tests.AEM.Base;

namespace Tests.AEM;

[TestClass]
public class PageDataHandlerTests : BaseDataHandlerTests
{
    protected override IAsyncDataSourceItemHandler DataHandler => new PageDataHandler(InvocationContext);

    protected override string SearchString => "Surf";
}
