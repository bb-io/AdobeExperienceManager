using Apps.AEM.Actions;
using Apps.AEM.Models.Requests;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Newtonsoft.Json;
using System.Reflection;
using Tests.AEM.Base;

namespace Tests.AEM;

[TestClass]
public class ContentActionsTests : TestBase
{
    // can't use parent method directly in DynamicData decorator as studio can't see it and shows a warning
    public static string? GetConnectionTypeName(MethodInfo _, object[]? data) => GetConnectionTypeFromDynamicData(data);

    [TestMethod]
    [DynamicData(nameof(AllInvocationContexts), DynamicDataDisplayName = nameof(GetConnectionTypeName))]
    public async Task SearchContent_NoParameters_ShouldReturnContent(InvocationContext context)
    {
        // Arrange
        var actions = new ContentActions(context, FileManager);
        
        // Act
        var result = await actions.SearchContent(new SearchContentRequest()
        {
            StartDate = DateTime.UtcNow.AddDays(-90), // Default is 31 days, so we extend it to ensure we get results from a test instance
        });
        
        // Assert
        Assert.IsTrue(result.Content.Any(), "No content items were returned");
        TestContext.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
    }
    
    [TestMethod]
    [DynamicData(nameof(AllInvocationContexts), DynamicDataDisplayName = nameof(GetConnectionTypeName))]
    public async Task SearchContent_WithRootPath_ShouldReturnFilteredContent(InvocationContext context)
    {
        // Arrange
        var actions = new ContentActions(context, FileManager);
        var request = new SearchContentRequest 
        {
            RootPath = "/content/experience-fragments/wknd/language-masters/fr/site/footer",
            StartDate = DateTime.UtcNow.AddDays(-360),
        };
        
        // Act
        var result = await actions.SearchContent(request);
        
        // Assert
        Assert.IsTrue(result.Content.Any(), "No content items were returned");
        Assert.IsTrue(result.Content.All(p => p.ContentId.StartsWith(request.RootPath)), 
            "Some returned content items don't match the specified root path");
        TestContext.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
    }

    [TestMethod]
    [DynamicData(nameof(AllInvocationContexts), DynamicDataDisplayName = nameof(GetConnectionTypeName))]
    public async Task DownloadContent_Interoperable_WithValidPath_ShouldReturnFileReference(InvocationContext context)
    {
        // Arrange
        var actions = new ContentActions(context, FileManager);
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
        
        TestContext.WriteLine($"Generated HTML file: {result.Content.Name}");
        TestContext.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
    }

    [TestMethod]
    [DynamicData(nameof(AllInvocationContexts), DynamicDataDisplayName = nameof(GetConnectionTypeName))]
    public async Task DownloadContent_Original_WithValidPath_ShouldReturnFileReference(InvocationContext context)
    {
        // Arrange
        var actions = new ContentActions(context, FileManager);
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
        
        TestContext.WriteLine($"Generated JSON file: {result.Content.Name}");
    }

    [TestMethod]
    [DynamicData(nameof(AllInvocationContexts), DynamicDataDisplayName = nameof(GetConnectionTypeName))]
    public async Task UploadContent_WithInteroperableHTMLInput_ShouldSucceed(InvocationContext context)
    {
        // Arrange
        var actions = new ContentActions(context, FileManager);
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
        TestContext.WriteLine(JsonConvert.SerializeObject(response, Formatting.Indented));
    }

    [TestMethod]
    [DynamicData(nameof(AllInvocationContexts), DynamicDataDisplayName = nameof(GetConnectionTypeName))]
    public async Task UploadContent_WithInteroperableXliffInput_ShouldSucceed(InvocationContext context)
    {
        // Arrange
        var actions = new ContentActions(context, FileManager);
        var request = new UploadContentRequest
        {
            Content = new FileReference
            {
                Name = "__content__wknd__us__en__magazine__san-diego-surf.html-en-ko-T-C.xlf",
                ContentType = "application/xliff+xml"
            },
            SourceLocale = "/en",
            Locale = "/ko",
            IgnoreReferenceContentErrors = true
        };

        // Act
        var response = await actions.UploadContent(request);

        // Assert
        Assert.IsNotNull(response, "Response should not be null");
        TestContext.WriteLine(JsonConvert.SerializeObject(response, Formatting.Indented));
    }

    [TestMethod]
    [DynamicData(nameof(AllInvocationContexts), DynamicDataDisplayName = nameof(GetConnectionTypeName))]
    public async Task UploadContent_WithValidOriginalInput_ShouldSucceed(InvocationContext context)
    {
        // Arrange
        var actions = new ContentActions(context, FileManager);
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
        TestContext.WriteLine(JsonConvert.SerializeObject(response, Formatting.Indented));
    }

    [TestMethod]
    [DynamicData(nameof(AllInvocationContexts), DynamicDataDisplayName = nameof(GetConnectionTypeName))]
    public async Task AddTags_ShouldBeSuccessful(InvocationContext context)
    {
        // Arrange
        var actions = new ContentActions(context, FileManager);
        var contentId = "/content/wknd/language-masters/nl/faqs";
        var tagId = "workflow:wcm/ready-for-translation";
        var addRequest = new ChangeTagsRequest
        {
            ContentPath = contentId,
            AddTags = [tagId]
        };

        // Act
        var addResult = await actions.ChangeTags(addRequest);

        // Assert
        Assert.IsTrue(addResult.Tags.Contains(tagId), "Tag was not added");
    }

    [TestMethod]
    [DynamicData(nameof(AllInvocationContexts), DynamicDataDisplayName = nameof(GetConnectionTypeName))]
    public async Task RemoveTags_ShouldBeSuccessful(InvocationContext context)
    {
        // Arrange
        var actions = new ContentActions(context, FileManager);
        var contentId = "/content/wknd/us/en/magazine/san-diego-surf";
        var tagId = "workflow:wcm/ready-for-translation";

        // Ensure tag exists first
        await actions.ChangeTags(new ChangeTagsRequest { ContentPath = contentId, AddTags = [tagId] });

        var removeRequest = new ChangeTagsRequest
        {
            ContentPath = contentId,
            RemoveTags = [tagId]
        };


        // Act
        var removeResult = await actions.ChangeTags(removeRequest);

        // Assert
        Assert.IsFalse(removeResult.Tags.Contains(tagId), "Tag was not removed");
    }

    [TestMethod]
    [DynamicData(nameof(AllInvocationContexts), DynamicDataDisplayName = nameof(GetConnectionTypeName))]
    public async Task AddAndRemoveTags_ShouldBeSuccessful(InvocationContext context)
    {
        // Arrange
        var actions = new ContentActions(context, FileManager);
        var contentId = "/content/wknd/us/en/magazine/san-diego-surf";
        var tagIdToAdd = "workflow:projects/wcm/landingpage";
        var tagIdToRemove = "workflow:wcm/ready-for-translation";

        // Ensure tag exists first
        await actions.ChangeTags(new ChangeTagsRequest { ContentPath = contentId, RemoveTags = [tagIdToAdd] });
        await actions.ChangeTags(new ChangeTagsRequest { ContentPath = contentId, AddTags = [tagIdToRemove] });

        var changeTagRequest = new ChangeTagsRequest
        {
            ContentPath = contentId,
            AddTags = [tagIdToAdd],
            RemoveTags = [tagIdToRemove]
        };

        // Act
        var bothResult = await actions.ChangeTags(changeTagRequest);

        // Assert
        Assert.IsTrue(bothResult.Tags.Contains(tagIdToAdd), "Tag was not added in combined request");
        Assert.IsFalse(bothResult.Tags.Contains(tagIdToRemove), "Tag was not removed in combined request");
    }

    [TestMethod]
    [DynamicData(nameof(AllInvocationContexts), DynamicDataDisplayName = nameof(GetConnectionTypeName))]
    public async Task AddAndRemoveTags_WithAssets_ShouldBeSuccessful(InvocationContext context)
    {
        // Arrange
        var actions = new ContentActions(context, FileManager);
        var contentId = "/content/dam/aem-demo-assets/en/guides/dita-sample-content_IT/Smart-security.ditamap";
        var tagIdsToAdd = new[] { "workflow:projects/wcm/landingpage", "workflow:projects/wcm/email" };
        var tagIdsToRemove = new[] { "workflow:wcm/ready-for-translation", "workflow:wcm/translation" };

        // Ensure tag exists first
        await actions.ChangeTags(new ChangeTagsRequest { ContentPath = contentId, RemoveTags = tagIdsToAdd });
        await actions.ChangeTags(new ChangeTagsRequest { ContentPath = contentId, AddTags = tagIdsToRemove });

        var changeTagRequest = new ChangeTagsRequest
        {
            ContentPath = contentId,
            AddTags = tagIdsToAdd,
            RemoveTags = tagIdsToRemove
        };

        // Act
        var bothResult = await actions.ChangeTags(changeTagRequest);

        // Assert
        Assert.IsTrue(bothResult.Tags.Intersect(tagIdsToAdd).Any(), "Tags were not added in combined request");
        Assert.IsFalse(bothResult.Tags.Intersect(tagIdsToRemove).Any(), "Tags were not removed in combined request");
    }
}