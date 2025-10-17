using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using System.Reflection;

namespace Tests.AEM.Base;

public abstract class BaseDataHandlerTests : TestBase
{
    protected abstract IAsyncDataSourceItemHandler CreateDataHandler(InvocationContext context);
    protected abstract string SearchString { get; }
    protected virtual bool CanBeEmpty => false;

    // can't use parent method directly in DynamicData decorator as studio can't see it and shows a warning
    public static string? GetConnectionTypeName(MethodInfo _, object[]? data) => GetConnectionTypeFromDynamicData(data);

    [TestMethod]
    [DynamicData(nameof(AllInvocationContexts), DynamicDataDisplayName = nameof(GetConnectionTypeName))]
    public virtual async Task GetDataAsync_WithoutSearchString_ShouldReturnNonEmptyCollection(InvocationContext context)
    {
        var dataHandler = CreateDataHandler(context);
        await TestDataHandlerAsync(dataHandler);
    }

    [TestMethod]
    [DynamicData(nameof(AllInvocationContexts), DynamicDataDisplayName = nameof(GetConnectionTypeName))]
    public virtual async Task GetDataAsync_WithSearchString_ShouldReturnNonEmptyCollection(InvocationContext context)
    {
        var dataHandler = CreateDataHandler(context);
        await TestDataHandlerAsync(dataHandler, SearchString);
    }

    private async Task TestDataHandlerAsync(IAsyncDataSourceItemHandler dataHandler, string? searchString = null)
    {
        var context = new DataSourceContext { SearchString = searchString };
        var result = await dataHandler.GetDataAsync(context, CancellationToken.None);

        Assert.IsNotNull(result);
        if(CanBeEmpty == false)
        {
            Assert.IsTrue(result.Any(), "Result should not be empty.");
        }

        Assert.IsTrue(result.All(item => !string.IsNullOrEmpty(item.DisplayName)), "All items should have a name.");
        if (!string.IsNullOrEmpty(searchString))
        {
            Assert.IsTrue(result.All(item => item.DisplayName.Contains(searchString, StringComparison.OrdinalIgnoreCase)), 
                $"All items should contain the search string '{searchString}'.");
        }

        LogItems(result);
    }

    private void LogItems(IEnumerable<DataSourceItem> items)
    {
        TestContext.WriteLine($"Total items: {items.Count()}");
        foreach (var item in items)
        {
            TestContext.WriteLine($"ID: {item.Value}, Name: {item.DisplayName}");
        }
    }
}
