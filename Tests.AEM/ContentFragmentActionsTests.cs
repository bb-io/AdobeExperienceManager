using Apps.AEM.Actions;
using Apps.AEM.Models.Requests;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Newtonsoft.Json;
using System.Reflection;
using System.Text;
using Tests.AEM.Base;

namespace Tests.AEM;

[TestClass]
public class ContentFragmentActionsTests : TestBase
{
    private const string ArticlesRootPath = "/content/dam/raye/fragments/articles/library";
    private const string SampleContentFragmentPath = "/content/dam/raye/fragments/articles/library/more-uk-pork-producers-given-green-light-to-restart-exports-to-china";
    private const string ArticleTag = "raye:market/country/united-kingom-of-great-britain-and-northern-ireland";
    private const string HtmlFixtureName = "content-fragment-article-translated.html";
    private const string XliffFixtureName = "content-fragment-article-translated.xlf";
    private const string HtmlVariationTitle = "Blackbird Html Test";
    private const string XliffVariationTitle = "Blackbird Xliff Test";

    public static string? GetConnectionTypeName(MethodInfo _, object[]? data) => GetConnectionTypeFromDynamicData(data);

    [TestMethod]
    [DynamicData(nameof(AllInvocationContexts), DynamicDataDisplayName = nameof(GetConnectionTypeName))]
    public async Task SearchContentFragments_WithRootPath_ShouldReturnFilteredContent(InvocationContext context)
    {
        var actions = new ContentFragmentActions(context, FileManager);
        var request = new SearchContentFragmentsRequest
        {
            RootPath = ArticlesRootPath,
            MaxItems = 100
        };

        var result = await actions.SearchContentFragments(request);
        TestContext.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));

        Assert.IsTrue(result.Items.Any(), "No content fragments were returned.");
        Assert.IsTrue(
            result.Items.All(item => item.ContentId.StartsWith(ArticlesRootPath, StringComparison.OrdinalIgnoreCase)),
            "Some returned content fragments do not match the specified root path.");
    }

    [TestMethod]
    [DynamicData(nameof(AllInvocationContexts), DynamicDataDisplayName = nameof(GetConnectionTypeName))]
    public async Task SearchContentFragments_WithTags_ShouldReturnFilteredContent(InvocationContext context)
    {
        var actions = new ContentFragmentActions(context, FileManager);
        var request = new SearchContentFragmentsRequest
        {
            RootPath = ArticlesRootPath,
            Tags = [ArticleTag],
            MaxItems = 100
        };

        var result = await actions.SearchContentFragments(request);
        TestContext.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));

        Assert.IsTrue(result.Items.Any(), "No content fragments were returned for the requested tag.");
    }

    [TestMethod]
    [DynamicData(nameof(AllInvocationContexts), DynamicDataDisplayName = nameof(GetConnectionTypeName))]
    public async Task DownloadContentFragment_WithValidPath_ShouldReturnHtmlFileReference(InvocationContext context)
    {
        var actions = new ContentFragmentActions(context, FileManager);

        var result = await actions.DownloadContentFragments(new DownloadContentFragmentRequest
        {
            ContentId = SampleContentFragmentPath
        });

        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Content);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.Content.Name), "File name should not be empty.");
        Assert.AreEqual("text/html", result.Content.ContentType, "File content type should be text/html.");
        var html = ReadGeneratedOutput(result.Content.Name);
        Assert.IsFalse(
            html.Contains("data-field-name=\"history\"", StringComparison.OrdinalIgnoreCase),
            "The generated HTML body should exclude the history field by default.");
        Assert.IsFalse(
            html.Contains("data-field-name=\"previewUrl\"", StringComparison.OrdinalIgnoreCase),
            "The generated HTML body should exclude the previewUrl field by default.");

        TestContext.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
    }

    [TestMethod]
    [DynamicData(nameof(AllInvocationContexts), DynamicDataDisplayName = nameof(GetConnectionTypeName))]
    public async Task DownloadContentFragment_WithExcludedFields_ShouldOmitRequestedFieldFromHtmlBody(InvocationContext context)
    {
        var actions = new ContentFragmentActions(context, FileManager);

        var result = await actions.DownloadContentFragments(new DownloadContentFragmentRequest
        {
            ContentId = SampleContentFragmentPath,
            ExcludedFields = ["gating"]
        });

        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Content);

        var html = ReadGeneratedOutput(result.Content.Name);
        Assert.IsFalse(
            html.Contains("data-field-name=\"gating\"", StringComparison.OrdinalIgnoreCase),
            "The generated HTML body should exclude explicitly requested fields.");
    }

    [TestMethod]
    [DynamicData(nameof(AllInvocationContexts), DynamicDataDisplayName = nameof(GetConnectionTypeName))]
    public async Task UploadContentFragment_WithHtmlInput_ShouldSucceed(InvocationContext context)
    {
        var actions = new ContentFragmentActions(context, FileManager);

        try
        {
            var result = await actions.UploadContentFragments(new UploadContentFragmentRequest
            {
                Content = BuildFileReference(HtmlFixtureName, "text/html"),
                VariationTitle = HtmlVariationTitle,
                VariationDescription = "Created by the Blackbird integration test.",
                ContentId = SampleContentFragmentPath
            });

            Assert.IsNotNull(result);
            Assert.AreEqual(SampleContentFragmentPath, result.ContentId);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.VariationName), "Variation name should not be empty.");
            Assert.IsTrue(
                result.Message.Contains("uploaded successfully", StringComparison.OrdinalIgnoreCase),
                "Upload response did not contain a success message.");

            TestContext.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
        }
        catch (PluginMisconfigurationException ex) when (ex.Message.Contains("checked out", StringComparison.OrdinalIgnoreCase))
        {
            Assert.Inconclusive($"The sample content fragment is currently locked in AEM: {ex.Message}");
        }
    }

    [TestMethod]
    [DynamicData(nameof(AllInvocationContexts), DynamicDataDisplayName = nameof(GetConnectionTypeName))]
    public async Task UploadContentFragment_WithXliffInput_ShouldSucceed(InvocationContext context)
    {
        var actions = new ContentFragmentActions(context, FileManager);

        try
        {
            var result = await actions.UploadContentFragments(new UploadContentFragmentRequest
            {
                Content = BuildFileReference(XliffFixtureName, "application/xliff+xml"),
                VariationTitle = XliffVariationTitle,
                VariationDescription = "Created by the Blackbird integration test from XLIFF.",
                ContentId = SampleContentFragmentPath
            });

            Assert.IsNotNull(result);
            Assert.AreEqual(SampleContentFragmentPath, result.ContentId);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.VariationName), "Variation name should not be empty.");
            Assert.IsTrue(
                result.Message.Contains("uploaded successfully", StringComparison.OrdinalIgnoreCase),
                "Upload response did not contain a success message.");

            TestContext.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
        }
        catch (PluginMisconfigurationException ex) when (ex.Message.Contains("checked out", StringComparison.OrdinalIgnoreCase))
        {
            Assert.Inconclusive($"The sample content fragment is currently locked in AEM: {ex.Message}");
        }
    }

    private static FileReference BuildFileReference(string name, string contentType)
    {
        return new FileReference
        {
            Name = name,
            ContentType = contentType
        };
    }

    private static string ReadGeneratedOutput(string fileName)
    {
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var projectDirectory = Directory.GetParent(baseDirectory)?.Parent?.Parent?.Parent?.FullName
            ?? throw new DirectoryNotFoundException("Project directory not found.");

        var path = Path.Combine(projectDirectory, "TestFiles", "Output", fileName);
        Assert.IsTrue(File.Exists(path), $"Generated file not found at: {path}");

        return File.ReadAllText(path, Encoding.UTF8);
    }
}
