using Apps.AEM.Handlers;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Models.FileDataSourceItems;
using System.Reflection;
using Tests.AEM.Base;

namespace Tests.AEM;

[TestClass]
public class ContentPickerDataSourceHandlerTests : TestBase
{
    // can't use parent method directly in DynamicData decorator as studio can't see it and shows a warning
    public static string? GetConnectionTypeName(MethodInfo _, object[]? data) => GetConnectionTypeFromDynamicData(data);

    [TestMethod]
    [DynamicData(nameof(AllInvocationContexts), DynamicDataDisplayName = nameof(GetConnectionTypeName))]
    public async Task GetFolderContentAsync_RootFolder_ReturnsItems(InvocationContext context)
    {
        // Arrange
        var handler = new ContentPickerDataSourceHandler(context);

        // Act
        var result = await handler.GetFolderContentAsync(new FolderContentDataSourceContext
        {
            FolderId = string.Empty
        }, CancellationToken.None);

        // Assert
        var itemList = result.ToList();
        Assert.IsNotNull(result, "Result should not be null");
        Assert.IsTrue(itemList.Count > 0, "The folder should contain items.");

        foreach (var item in itemList)
        {
            Console.WriteLine($"Item: {item.DisplayName}, Id: {item.Id}, Type: {(item is Folder ? "Folder" : "File")}");
        }
    }

    [TestMethod]
    [DynamicData(nameof(AllInvocationContexts), DynamicDataDisplayName = nameof(GetConnectionTypeName))]
    public async Task GetFolderContentAsync_SpecificFolder_ReturnsFilteredItems(InvocationContext context)
    {
        // Arrange
        var handler = new ContentPickerDataSourceHandler(context);
        var folderId = "/content/experience-fragments";

        // Act
        var result = await handler.GetFolderContentAsync(new FolderContentDataSourceContext
        {
            FolderId = folderId
        }, CancellationToken.None);

        // Assert
        var itemList = result.ToList();
        Assert.IsNotNull(result, "Result should not be null");

        foreach (var item in itemList)
        {
            Assert.IsTrue(item.Id.StartsWith(folderId), $"Item {item.Id} should start with {folderId}");
            Console.WriteLine($"Item: {item.DisplayName}, Id: {item.Id}, Type: {(item is Folder ? "Folder" : "File")}");
        }
    }

    [TestMethod]
    [DynamicData(nameof(AllInvocationContexts), DynamicDataDisplayName = nameof(GetConnectionTypeName))]
    public async Task GetFolderContentAsync_DeepFolder_ReturnsFilesAndFolders(InvocationContext context)
    {
        // Arrange
        var handler = new ContentPickerDataSourceHandler(context);
        var folderId = "/content/wknd";

        // Act
        var result = await handler.GetFolderContentAsync(new FolderContentDataSourceContext
        {
            FolderId = folderId
        }, CancellationToken.None);

        // Assert
        var itemList = result.ToList();
        Assert.IsNotNull(result, "Result should not be null");

        var folders = itemList.OfType<Folder>().ToList();
        var files = itemList.OfType<Blackbird.Applications.SDK.Extensions.FileManagement.Models.FileDataSourceItems.File>().ToList();

        Console.WriteLine($"Total items: {itemList.Count}, Folders: {folders.Count}, Files: {files.Count}");

        foreach (var item in itemList)
        {
            Console.WriteLine($"Item: {item.DisplayName}, Id: {item.Id}, Type: {(item is Folder ? "Folder" : "File")}, Selectable: {item.IsSelectable}");
        }

        // Folders should not be selectable, files should be
        Assert.IsTrue(folders.All(f => !f.IsSelectable), "Folders should not be selectable");
        Assert.IsTrue(files.All(f => f.IsSelectable), "Files should be selectable");
    }

    [TestMethod]
    [DynamicData(nameof(AllInvocationContexts), DynamicDataDisplayName = nameof(GetConnectionTypeName))]
    public async Task GetFolderPathAsync_EmptyContext_ReturnsRoot(InvocationContext context)
    {
        // Arrange
        var handler = new ContentPickerDataSourceHandler(context);

        // Act
        var result = await handler.GetFolderPathAsync(new FolderPathDataSourceContext
        {
            FileDataItemId = null
        }, CancellationToken.None);

        // Assert
        var pathList = result.ToList();
        Assert.IsNotNull(result, "Result should not be null");
        Assert.AreEqual(1, pathList.Count, "Should return only root");
        Assert.AreEqual("/content", pathList[0].Id, "Root should be /content");
        Assert.AreEqual("Content", pathList[0].DisplayName, "Root display name should be 'Content'");

        Console.WriteLine($"Path: {pathList[0].DisplayName} ({pathList[0].Id})");
    }

    [TestMethod]
    [DynamicData(nameof(AllInvocationContexts), DynamicDataDisplayName = nameof(GetConnectionTypeName))]
    public async Task GetFolderPathAsync_DeepPath_ReturnsBreadcrumbs(InvocationContext context)
    {
        // Arrange
        var handler = new ContentPickerDataSourceHandler(context);
        var itemId = "/content/experience-fragments/wknd/language-masters/en/site/sign-in";

        // Act
        var result = await handler.GetFolderPathAsync(new FolderPathDataSourceContext
        {
            FileDataItemId = itemId
        }, CancellationToken.None);

        // Assert
        var pathList = result.ToList();
        Assert.IsNotNull(result, "Result should not be null");
        Assert.IsTrue(pathList.Count > 1, "Should return multiple breadcrumb items");
        Assert.AreEqual("/content", pathList[0].Id, "First item should be root");

        Console.WriteLine("Breadcrumb path:");
        foreach (var item in pathList)
        {
            Console.WriteLine($"  {item.DisplayName} ({item.Id})");
        }

        // Verify the path is correct
        Assert.AreEqual("Content", pathList[0].DisplayName);
        Assert.IsTrue(pathList.Any(p => p.DisplayName == "experience-fragments"));
        Assert.IsTrue(pathList.Any(p => p.DisplayName == "wknd"));
    }
}
