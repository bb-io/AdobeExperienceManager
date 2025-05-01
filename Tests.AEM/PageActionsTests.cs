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
        var actions = new PageActions(InvocationContext);
        
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
        var actions = new PageActions(InvocationContext);
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
}
