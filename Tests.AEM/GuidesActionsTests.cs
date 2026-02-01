using Apps.AEM.Actions;
using Apps.AEM.Models.Requests;
using Blackbird.Applications.Sdk.Common.Invocation;
using System.Reflection;
using Tests.AEM.Base;

namespace Tests.AEM;

[TestClass]
public class GuidesActionsTests : TestBase
{
    // can't use parent method directly in DynamicData decorator as studio can't see it and shows a warning
    public static string? GetConnectionTypeName(MethodInfo _, object[]? data) => GetConnectionTypeFromDynamicData(data);

    [TestMethod]
    [DynamicData(nameof(AllInvocationContexts), DynamicDataDisplayName = nameof(GetConnectionTypeName))]
    public async Task SearchContent_Ditatopic_ShouldSucceed(InvocationContext context)
    {
        // Arrange
        var actions = new GuidesActions(context, FileManager);
        var contentId = "/content/dam/aem-demo-assets/en/guides/dita-sample-content_IT/topics/user-rights.dita";
        var request = new SearchMappedContentRequest
        {
            ContentIds = [contentId]
        };

        // Act
        var result = await actions.SearchReferencedContent(request);

        // Assert
        PrintResult(result);

        Assert.AreEqual(1, result.ContentIds.Count());
        Assert.Contains(contentId, result.ContentIds);
        Assert.AreEqual(0, result.Errors.Count());
    }

    [TestMethod]
    [DynamicData(nameof(AllInvocationContexts), DynamicDataDisplayName = nameof(GetConnectionTypeName))]
    public async Task SearchContent_Ditamap_ShouldSucceed(InvocationContext context)
    {
        // Arrange
        var actions = new GuidesActions(context, FileManager);
        var contentId = "/content/dam/aem-demo-assets/en/guides/dita-sample-content_IT/Smart-security.ditamap";
        var request = new SearchMappedContentRequest
        {
            ContentIds = [contentId]
        };

        // Act
        var result = await actions.SearchReferencedContent(request);

        // Assert
        PrintResult(result);

        Assert.AreEqual(36, result.ContentIds.Count());
        Assert.Contains(contentId, result.ContentIds);
        Assert.Contains("/content/dam/aem-demo-assets/en/guides/dita-sample-content_IT/resources/it-keys.ditamap", result.ContentIds);
        Assert.Contains("/content/dam/aem-demo-assets/en/guides/dita-sample-content_IT/topics/connecting-security-devices-to-virtual-map.dita", result.ContentIds);
        Assert.AreEqual(0, result.Errors.Count());
    }

    [TestMethod]
    [DynamicData(nameof(AllInvocationContexts), DynamicDataDisplayName = nameof(GetConnectionTypeName))]
    public async Task SearchContent_DitamapExcludedTags_ShouldSucceed(InvocationContext context)
    {
        // Arrange
        var actions = new GuidesActions(context, FileManager);
        var contentId = "/content/dam/aem-demo-assets/en/guides/dita-sample-content_IT/Smart-security.ditamap";
        var requestAll = new SearchMappedContentRequest
        {
            ContentIds = [contentId],
        };
        var requestExcludedTags = new SearchMappedContentRequest
        {
            ContentIds = [contentId],
            ExcludedTags = ["do-not-translate"]
        };

        // Act
        var resultAll = await actions.SearchReferencedContent(requestAll);
        var resultExcludedTags = await actions.SearchReferencedContent(requestExcludedTags);

        // Assert
        PrintResult(resultAll);
        PrintResult(resultExcludedTags);

        Assert.IsGreaterThan(resultExcludedTags.ContentIds.Count(), resultAll.ContentIds.Count());
        Assert.AreEqual(0, resultAll.Errors.Count());
        Assert.AreEqual(0, resultExcludedTags.Errors.Count());
    }

    [TestMethod]
    [DynamicData(nameof(AllInvocationContexts), DynamicDataDisplayName = nameof(GetConnectionTypeName))]
    public async Task SearchContent_Ditabook_ShouldSucceed(InvocationContext context)
    {
        // Arrange
        var actions = new GuidesActions(context, FileManager);
        var contentId = "/content/dam/aem-demo-assets/en/guides/book.ditamap";
        var request = new SearchMappedContentRequest
        {
            ContentIds = [contentId]
        };

        // Act
        var result = await actions.SearchReferencedContent(request);

        // Assert
        PrintResult(result);

        Assert.AreEqual(154, result.ContentIds.Count());
        Assert.Contains(contentId, result.ContentIds);
        Assert.Contains("/content/dam/aem-demo-assets/en/guides/dita-sample-content_IT/topics/user-rights.dita", result.ContentIds);
        Assert.Contains("/content/dam/aem-demo-assets/en/guides/dita-sample-content-manufacturing-machinery/glossentries/RC-servo.dita", result.ContentIds);
        Assert.Contains("/content/dam/aem-demo-assets/en/guides/dita-sample-content-car-insurance/topics/international/Car-insurance-Hebrew.dita", result.ContentIds);

        Assert.AreEqual(8, result.Errors.Count());
        Assert.Contains("Content ID: /content/dam/aem-demo-assets/en/guides/dita-sample-content-manufacturing-machinery/images/Declaration-of-Conformity-EPGR-S.pdf is an asset. Assets not supported.", result.Errors);
        Assert.Contains("Failed to resolve GUID reference for GUID-3ce46f47-b657-411c-88dd-0bd6876ff312 (referred in /content/dam/aem-demo-assets/en/guides/dita-sample-content-car-insurance/car-insurance-knowledge-base.ditamap).", result.Errors);
    }

    [TestMethod]
    [DynamicData(nameof(AllInvocationContexts), DynamicDataDisplayName = nameof(GetConnectionTypeName))]
    public async Task SearchContent_DitabookWithoutRecursive_ShouldSucceed(InvocationContext context)
    {
        // Arrange
        var actions = new GuidesActions(context, FileManager);
        var contentId = "/content/dam/aem-demo-assets/en/guides/book.ditamap";
        var request = new SearchMappedContentRequest
        {
            ContentIds = [contentId],
            SearchRecursively = false,
        };

        // Act
        var result = await actions.SearchReferencedContent(request);

        // Assert
        PrintResult(result);

        Assert.AreEqual(6, result.ContentIds.Count());
        Assert.Contains(contentId, result.ContentIds);

        Assert.AreEqual(0, result.Errors.Count());
    }

    [TestMethod]
    [DynamicData(nameof(AllInvocationContexts), DynamicDataDisplayName = nameof(GetConnectionTypeName))]
    public async Task SearchContent_DitabookWithoutRecursiveWithoutMaps_ShouldSucceed(InvocationContext context)
    {
        // Arrange
        var actions = new GuidesActions(context, FileManager);
        var contentId = "/content/dam/aem-demo-assets/en/guides/book.ditamap";
        var request = new SearchMappedContentRequest
        {
            ContentIds = [contentId],
            SearchRecursively = false,
            IncludeMaps = false,
        };

        // Act
        var result = await actions.SearchReferencedContent(request);

        // Assert
        PrintResult(result);

        Assert.AreEqual(5, result.ContentIds.Count());
        Assert.DoesNotContain(contentId, result.ContentIds);

        Assert.AreEqual(0, result.Errors.Count());
    }

    [TestMethod]
    [DynamicData(nameof(AllInvocationContexts), DynamicDataDisplayName = nameof(GetConnectionTypeName))]
    public async Task SearchContent_MultipleDitamaps_ShouldSucceed(InvocationContext context)
    {
        // Arrange
        var actions = new GuidesActions(context, FileManager);
        var request = new SearchMappedContentRequest
        {
            ContentIds =
            [
                "/content/dam/aem-demo-assets/en/guides/dita-sample-content_IT/Smart-security.ditamap",
                "/content/dam/aem-demo-assets/en/guides/dita-sample-content-car-insurance/car-insurance-knowledge-base.ditamap",
                "/content/dam/aem-demo-assets/en/guides/dita-sample-content-manufacturing-machinery/Electro-pneumatic-gantry-robot.ditamap",
            ]
        };

        // Act
        var result = await actions.SearchReferencedContent(request);

        // Assert
        PrintResult(result);

        Assert.AreEqual(121, result.ContentIds.Count());
        Assert.Contains("/content/dam/aem-demo-assets/en/guides/dita-sample-content_IT/topics/intended-use.dita", result.ContentIds);
        Assert.Contains("/content/dam/aem-demo-assets/en/guides/dita-sample-content-car-insurance/topics/About.dita", result.ContentIds);
        Assert.Contains("/content/dam/aem-demo-assets/en/guides/dita-sample-content-manufacturing-machinery/topics/Overview-of-components.dita", result.ContentIds);

        Assert.AreEqual(8, result.Errors.Count());
    }

    [TestMethod]
    [DynamicData(nameof(AllInvocationContexts), DynamicDataDisplayName = nameof(GetConnectionTypeName))]
    public async Task DownloadContent_Ditatopic_ShouldSucceed(InvocationContext context)
    {
        // Arrange
        var actions = new GuidesActions(context, FileManager);
        var request = new DownloadContentRequest
        {
            ContentId = "/content/dam/aem-demo-assets/en/guides/dita-sample-content_IT/topics/user-rights.dita",
            FileFormat = "original",
            IncludeReferenceContent = true,
        };

        // Act
        var result = await actions.DownloadDitaContent(request);

        // Assert
        Assert.IsNotNull(result, "Response should not be null");
        Assert.IsNotNull(result.Content, "File reference should not be null");
        Assert.IsFalse(string.IsNullOrEmpty(result.Content.Name), "File name should not be empty");
        Assert.AreEqual("application/xml", result.Content.ContentType, "File content type should be application/xml");

        TestContext?.WriteLine($"Generated DITA file: {result.Content.Name}");
    }

    [TestMethod]
    [DynamicData(nameof(AllInvocationContexts), DynamicDataDisplayName = nameof(GetConnectionTypeName))]
    public async Task DownloadContent_Ditamap_ShouldSucceed(InvocationContext context)
    {
        // Arrange
        var actions = new GuidesActions(context, FileManager);
        var request = new DownloadContentRequest
        {
            ContentId = "/content/dam/aem-demo-assets/en/guides/dita-sample-content_IT/Smart-security.ditamap",
            FileFormat = "original",
            IncludeReferenceContent = true,
        };

        // Act
        var result = await actions.DownloadDitaContent(request);

        // Assert
        Assert.IsNotNull(result, "Response should not be null");
        Assert.IsNotNull(result.Content, "File reference should not be null");
        Assert.IsFalse(string.IsNullOrEmpty(result.Content.Name), "File name should not be empty");
        Assert.AreEqual("application/xml", result.Content.ContentType, "File content type should be application/xml");

        TestContext?.WriteLine($"Generated DITA file: {result.Content.Name}");
    }

    [TestMethod]
    [DynamicData(nameof(AllInvocationContexts), DynamicDataDisplayName = nameof(GetConnectionTypeName))]
    public async Task DownloadContent_Ditabook_ShouldSucceed(InvocationContext context)
    {
        // Arrange
        var actions = new GuidesActions(context, FileManager);
        var request = new DownloadContentRequest
        {
            ContentId = "/content/dam/aem-demo-assets/en/guides/book.ditamap",
            FileFormat = "original",
            IncludeReferenceContent = true,
        };

        // Act
        var result = await actions.DownloadDitaContent(request);

        // Assert
        Assert.IsNotNull(result, "Response should not be null");
        Assert.IsNotNull(result.Content, "File reference should not be null");
        Assert.IsFalse(string.IsNullOrEmpty(result.Content.Name), "File name should not be empty");
        Assert.AreEqual("application/xml", result.Content.ContentType, "File content type should be application/xml");

        TestContext?.WriteLine($"Generated DITA file: {result.Content.Name}");
    }
}
