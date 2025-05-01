using Apps.AEM.Actions;
using Apps.AEM.Models.Requests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Tests.AEM.Base;

namespace Tests.AEM;

[TestClass]
public class PageActionsTests : TestBase
{
    [TestMethod]
    public async Task SearchPagesAsync_NoParameters_ShouldReturnPages()
    {
        // Arrange
        var actions = new PageActions(InvocationContext, FileManager);
        
        // Act
        var result = await actions.SearchPagesAsync(new SearchPagesRequest());
        
        // Assert
        Assert.IsTrue(result.Pages.Any(), "No pages were returned");
        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
    }
    
    [TestMethod]
    public async Task SearchPagesAsync_WithRootPath_ShouldReturnFilteredPages()
    {
        // Arrange
        var actions = new PageActions(InvocationContext, FileManager);
        var request = new SearchPagesRequest 
        {
            RootPath = "/content/bb-aem-connector"
        };
        
        // Act
        var result = await actions.SearchPagesAsync(request);
        
        // Assert
        Assert.IsTrue(result.Pages.Any(), "No pages were returned");
        Assert.IsTrue(result.Pages.All(p => p.Path.StartsWith(request.RootPath)), 
            "Some returned pages don't match the specified root path");
        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
    }

    [TestMethod]
    public async Task GetPageAsHtmlAsync_WithValidPath_ShouldReturnFileReference()
    {
        // Arrange
        var actions = new PageActions(InvocationContext, FileManager);
        var request = new PageRequest
        {
            PagePath = "/content/bb-aem-connector/us/en/clear-skies"
        };
        
        // Act
        var result = await actions.GetPageAsHtmlAsync(request);
        
        // Assert
        Assert.IsNotNull(result, "Response should not be null");
        Assert.IsNotNull(result.File, "File reference should not be null");
        Assert.IsFalse(string.IsNullOrEmpty(result.File.Name), "File name should not be empty");
        Assert.AreEqual("text/html", result.File.ContentType, "File content type should be text/html");
        
        Console.WriteLine($"Generated HTML file: {result.File.Name}");
    }
}
