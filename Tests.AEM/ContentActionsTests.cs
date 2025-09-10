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
    public async Task SearchContent_NoParameters_ShouldReturnContent()
    {
        // Arrange
        var actions = new ContentActions(InvocationContext, FileManager);
        
        // Act
        var result = await actions.SearchContent(new SearchContentRequest()
        {
            StartDate = DateTime.UtcNow.AddDays(-90), // Default is 31 days, so we extend it to ensure we get results from a test instance
        });
        
        // Assert
        Assert.IsTrue(result.Content.Any(), "No content items were returned");
        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
    }
    
    [TestMethod]
    public async Task SearchContent_WithRootPath_ShouldReturnFilteredContent()
    {
        // Arrange
        var actions = new ContentActions(InvocationContext, FileManager);
        var request = new SearchContentRequest 
        {
            RootPath = "/content/wknd/us/en/magazine/",
            StartDate = DateTime.UtcNow.AddDays(-90),
        };
        
        // Act
        var result = await actions.SearchContent(request);
        
        // Assert
        Assert.IsTrue(result.Content.Any(), "No content items were returned");
        Assert.IsTrue(result.Content.All(p => p.ContentId.StartsWith(request.RootPath)), 
            "Some returned content items don't match the specified root path");
        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
    }

    [TestMethod]
    public async Task DownloadContent_Interoperable_WithValidPath_ShouldReturnFileReference()
    {
        // Arrange
        var actions = new ContentActions(InvocationContext, FileManager);
        var request = new DownloadContentRequest
        {
            ContentId = "/content/wknd/language-masters/en/magazine/western-australia",
            IncludeReferenceContent = true,
            SkipReferenceContentPaths =
            [
                "/content/experience-fragments/wknd/language-masters/en/site/header/master",
                "/content/experience-fragments/wknd/language-masters/en/site/footer/master",
            ],
        };
        
        // Act
        var result = await actions.DownloadContent(request);
        
        // Assert
        Assert.IsNotNull(result, "Response should not be null");
        Assert.IsNotNull(result.Content, "File reference should not be null");
        Assert.IsFalse(string.IsNullOrEmpty(result.Content.Name), "File name should not be empty");
        Assert.AreEqual("text/html", result.Content.ContentType, "File content type should be text/html");
        
        Console.WriteLine($"Generated HTML file: {result.Content.Name}");
        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
    }

    [TestMethod]
    public async Task DownloadContent_Original_WithValidPath_ShouldReturnFileReference()
    {
        // Arrange
        var actions = new ContentActions(InvocationContext, FileManager);
        var request = new DownloadContentRequest
        {
            ContentId = "/content/wknd/language-masters/en/magazine/western-australia",
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
    public async Task UploadContent_WithValidHTMLInput_ShouldSucceed()
    {
        // Arrange
        var actions = new ContentActions(InvocationContext, FileManager);
        var request = new UploadContentRequest
        {
            Content = new FileReference
            {
                Name = "__content__wknd__language-masters__en__faqs.html",
                ContentType = "text/html"
            },
            SourceLocale = "/en",
            Locale = "/nl",
            IgnoreReferenceContentErrors = true
        };
        
        // Act
        var response = await actions.UploadContent(request);

        // Assert
        Assert.IsNotNull(response, "Response should not be null");
        System.Console.WriteLine(JsonConvert.SerializeObject(response, Formatting.Indented));
    }

    [TestMethod]
    public async Task UploadContent_WithValidOriginalInput_ShouldSucceed()
    {
        // Arrange
        var actions = new ContentActions(InvocationContext, FileManager);
        var request = new UploadContentRequest
        {
            Content = new FileReference
            {
                Name = "__content__wknd__language-masters__en__faqs.json",
                ContentType = "application/json"
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