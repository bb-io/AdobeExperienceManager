using Apps.AEM.Actions;
using Apps.AEM.Models.Requests;
using Blackbird.Applications.Sdk.Common.Files;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Tests.AEM.Base;

namespace Tests.AEM;

[TestClass]
public class ContentActionsTests : TestBase
{
    [TestMethod]
    public async Task SearchPagesAsync_NoParameters_ShouldReturnPages()
    {
        // Arrange
        var actions = new ContentActions(InvocationContext, FileManager);
        
        // Act
        var result = await actions.SearchPagesAsync(new SearchContentRequest());
        
        // Assert
        Assert.IsTrue(result.Content.Any(), "No pages were returned");
        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
    }
    
    [TestMethod]
    public async Task SearchPagesAsync_WithRootPath_ShouldReturnFilteredPages()
    {
        // Arrange
        var actions = new ContentActions(InvocationContext, FileManager);
        var request = new SearchContentRequest 
        {
            RootPath = "/content/bb-aem-connector"
        };
        
        // Act
        var result = await actions.SearchPagesAsync(request);
        
        // Assert
        Assert.IsTrue(result.Content.Any(), "No pages were returned");
        Assert.IsTrue(result.Content.All(p => p.ContentId.StartsWith(request.RootPath)), 
            "Some returned pages don't match the specified root path");
        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
    }

    [TestMethod]
    public async Task DownloadContent_Interoperable_WithValidPath_ShouldReturnFileReference()
    {
        // Arrange
        var actions = new ContentActions(InvocationContext, FileManager);
        var request = new DownloadContentRequest
        {
            ContentId = "/content/wknd/language-masters/en/faqs",
            IncludeReferenceContent = true
        };
        
        // Act
        var result = await actions.DownloadContent(request);
        
        // Assert
        Assert.IsNotNull(result, "Response should not be null");
        Assert.IsNotNull(result.Content, "File reference should not be null");
        Assert.IsFalse(string.IsNullOrEmpty(result.Content.Name), "File name should not be empty");
        Assert.AreEqual("text/html", result.Content.ContentType, "File content type should be text/html");
        
        Console.WriteLine($"Generated HTML file: {result.Content.Name}");
    }

    [TestMethod]
    public async Task DownloadContent_Original_WithValidPath_ShouldReturnFileReference()
    {
        // Arrange
        var actions = new ContentActions(InvocationContext, FileManager);
        var request = new DownloadContentRequest
        {
            ContentId = "/content/wknd/language-masters/en/faqs",
            IncludeReferenceContent = true,
            FileFormat = "original"
        };
        
        // Act
        var result = await actions.DownloadContent(request);
        
        // Assert
        Assert.IsNotNull(result, "Response should not be null");
        Assert.IsNotNull(result.Content, "File reference should not be null");
        Assert.IsFalse(string.IsNullOrEmpty(result.Content.Name), "File name should not be empty");
        Assert.AreEqual("application/json", result.Content.ContentType, "File content type should be application/json");
        
        Console.WriteLine($"Generated JSON file: {result.Content.Name}");
    }

    [TestMethod]
    public async Task UpdatePageFromHtmlAsync_WithValidInput_ShouldSucceed()
    {
        // Arrange
        var actions = new ContentActions(InvocationContext, FileManager);
        var request = new UploadContentRequest
        {
            Content = new FileReference
            {
                Name = "About Us.html",
                ContentType = "text/html"
            },
            SourceLocale = "/en",
            Locale = "/fr",
            IgnoreReferenceContentErrors = true
        };
        
        // Act
        var response = await actions.UploadContent(request);

        // Assert
        Assert.IsNotNull(response, "Response should not be null");
        System.Console.WriteLine(JsonConvert.SerializeObject(response, Formatting.Indented));
    }
}