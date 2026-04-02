using Apps.AEM.Handlers;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using System.Reflection;
using Tests.AEM.Base;

namespace Tests.AEM;

[TestClass]
public class ContentFragmentDataHandlerTests : BaseDataHandlerTests
{
    protected override IAsyncDataSourceItemHandler CreateDataHandler(InvocationContext context) => new ContentFragmentDataHandler(context);

    protected override string SearchString => "india";

    public static string? GetDisplayName(MethodInfo _, object[]? data) => GetConnectionTypeFromDynamicData(data);

    [TestMethod]
    [DynamicData(nameof(AllInvocationContexts), DynamicDataDisplayName = nameof(GetDisplayName))]
    public async Task GetDataAsync_WithoutSearchString_ShouldReturnAtMostTwentyContentFragments(InvocationContext context)
    {
        var handler = new ContentFragmentDataHandler(context);

        var result = (await handler.GetDataAsync(new DataSourceContext(), CancellationToken.None)).ToList();

        Assert.IsTrue(result.Count <= 20, "The content fragment picker should return at most 20 items.");
        Assert.IsTrue(
            result.All(item => item.Value.StartsWith("/content/dam", StringComparison.OrdinalIgnoreCase)),
            "All content fragment picker values should be DAM paths.");
    }
}
