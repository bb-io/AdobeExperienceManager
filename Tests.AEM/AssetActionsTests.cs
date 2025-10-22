using Apps.AEM.Actions;
using Apps.AEM.Models.Requests;
using Blackbird.Applications.Sdk.Common.Invocation;
using System.Reflection;
using Tests.AEM.Base;

namespace Tests.AEM;

[TestClass]
public class AssetActionsTests : TestBase
{
    // can't use parent method directly in DynamicData decorator as studio can't see it and shows a warning
    public static string? GetConnectionTypeName(MethodInfo _, object[]? data) => GetConnectionTypeFromDynamicData(data);

    [TestMethod]
    [DynamicData(nameof(AllInvocationContexts), DynamicDataDisplayName = nameof(GetConnectionTypeName))]
    public async Task SearchAssets_ReturnsResults(InvocationContext context)
    {
        // Arrange
        var actions = new AssetActions(context, FileManager);
        var request = new SearchAssetsRequest()
        {
            RootPath = "/content/dam/some-folder",
        };

        // Act
        var result = await actions.SearchAssets(request);

        // Assert
        Assert.IsTrue(result.Assets.Any(), "No content items were returned");
        PrintResult(result);
    }

    [TestMethod]
    [DynamicData(nameof(AllInvocationContexts), DynamicDataDisplayName = nameof(GetConnectionTypeName))]
    public async Task DownloadAssetMetadata_DownloadsFile(InvocationContext context)
    {
        // Arrange
        var actions = new AssetActions(context, FileManager);
        var request = new AssetPathRequest()
        {
            Path = "/content/dam/some.dita",
        };

        // Act
        var result = await actions.DownloadAssetMetadata(request);

        // Assert
        Assert.IsNotNull(result.File, "No metadata was returned");
        PrintResult(result);
    }

    [TestMethod]
    [DynamicData(nameof(AllInvocationContexts), DynamicDataDisplayName = nameof(GetConnectionTypeName))]
    public async Task DownloadAsset_DownloadsFile(InvocationContext context)
    {
        // Arrange
        var actions = new AssetActions(context, FileManager);
        var request = new AssetPathRequest()
        {
            Path = "/content/dam/some.dita",
        };

        // Act
        var result = await actions.DownloadAsset(request);

        // Assert
        Assert.IsNotNull(result.File, "No metadata was returned");
        PrintResult(result);
    }
}
