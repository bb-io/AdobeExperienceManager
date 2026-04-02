using Apps.AEM.Models.Entities;
using Apps.AEM.Utils.Converters.InteroperableContent;
using HtmlAgilityPack;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System.Text;
using Tests.AEM.Base;

namespace Tests.AEM;

[TestClass]
public class ContentFragmentHtmlConverterTests : TestBase
{
    private const string FixtureName = "content-fragment-response-formatted.json";

    [TestMethod]
    public void ConvertToHtml_WithSampleFixture_ProducesExpectedRootAndReferenceBlocks()
    {
        var entities = BuildSampleEntities();

        var html = ContentFragmentHtmlConverter.ConvertToHtml(entities);

        Assert.IsTrue(html.Contains("data-root=\"true\"", StringComparison.OrdinalIgnoreCase), "Root block should be present.");
        Assert.IsTrue(html.Contains("data-reference-path=", StringComparison.OrdinalIgnoreCase), "Reference blocks should be present.");
        Assert.IsFalse(html.Contains("data-field-name=\"history\"", StringComparison.OrdinalIgnoreCase), "History should be excluded by default.");
        Assert.IsFalse(html.Contains("data-field-name=\"previewUrl\"", StringComparison.OrdinalIgnoreCase), "Preview URL should be excluded by default.");
        Assert.IsTrue(html.Contains("<p>This Q&amp;A summarises", StringComparison.Ordinal), "Rich text reference content should remain HTML.");

        var document = new HtmlDocument();
        document.LoadHtml(html);
        var bodyChildren = document.DocumentNode.SelectNodes("//body/div")
            ?? throw new AssertFailedException("Generated HTML did not contain fragment blocks.");

        var emittedPaths = bodyChildren
            .Select(node => node.GetAttributeValue("data-root", string.Empty).Equals("true", StringComparison.OrdinalIgnoreCase)
                ? node.GetAttributeValue("data-source-path", string.Empty)
                : node.GetAttributeValue("data-reference-path", string.Empty))
            .ToList();

        CollectionAssert.AreEqual(
            entities.Select(entity => entity.SourcePath).ToList(),
            emittedPaths,
            "Content fragment blocks should be emitted in the provided order.");
    }

    [TestMethod]
    public void ConvertToHtml_ThenHtmlToJson_WithSampleFixture_RoundTripsRootAndReferences()
    {
        var entities = BuildSampleEntities();
        var html = ContentFragmentHtmlConverter.ConvertToHtml(entities);

        var roundTrippedEntities = HtmlToJsonConverter.ConvertToJson(html);

        Assert.AreEqual(entities.Count, roundTrippedEntities.Count, "Expected the same number of entities after round-tripping.");
        Assert.IsFalse(roundTrippedEntities[0].ReferenceContent, "The first entity should be the root content fragment.");
        Assert.IsTrue(roundTrippedEntities.Skip(1).All(entity => entity.ReferenceContent), "All subsequent entities should be reference content.");

        CollectionAssert.AreEqual(
            entities.Select(entity => entity.SourcePath).ToList(),
            roundTrippedEntities.Select(entity => entity.SourcePath).ToList(),
            "Entity order should match the generated HTML block order.");

        Assert.AreEqual(
            "India educational series: Medical device registration and post-market compliance",
            roundTrippedEntities[0].TargetContent["fields"]?[3]?["values"]?[0]?.ToString(),
            "Root title should round-trip correctly.");
        Assert.AreEqual(
            "Morulaa HealthTech",
            roundTrippedEntities[1].TargetContent["fields"]?[0]?["values"]?[0]?.ToString(),
            "Author reference text should round-trip correctly.");
        Assert.AreEqual(
            "h4",
            roundTrippedEntities[2].TargetContent["fields"]?[1]?["values"]?[0]?.ToString(),
            "Enumeration values should round-trip correctly.");

        var richText = roundTrippedEntities[2].TargetContent["fields"]?[2]?["values"]?[0]?.ToString();
        Assert.IsNotNull(richText, "Rich text should be preserved after round-tripping.");
        Assert.IsTrue(richText.Contains("<p>This Q&amp;A summarises", StringComparison.Ordinal), "Rich text content should remain HTML.");
    }

    private List<ContentFragmentHtmlEntity> BuildSampleEntities()
    {
        var fixture = LoadFixture();
        var rootPath = fixture["path"]?.ToString()
            ?? throw new AssertFailedException("Fixture did not contain a root content fragment path.");
        var rootFields = fixture["fields"] as JArray
            ?? throw new AssertFailedException("Fixture did not contain root fields.");

        var references = fixture["variations"]?
            .Children<JObject>()
            .FirstOrDefault(variation => string.Equals(variation["name"]?.ToString(), "en_GB", StringComparison.Ordinal))
            ?["references"]?
            .Children<JObject>()
            .ToList()
            ?? throw new AssertFailedException("Fixture did not contain en_GB variation references.");

        var authorReference = references.FirstOrDefault(reference => string.Equals(reference["fieldName"]?.ToString(), "author", StringComparison.Ordinal))
            ?? throw new AssertFailedException("Fixture did not contain an author reference.");
        var articleContentReference = references.FirstOrDefault(reference => string.Equals(reference["fieldName"]?.ToString(), "articleContent", StringComparison.Ordinal))
            ?? throw new AssertFailedException("Fixture did not contain an articleContent reference.");
        var countryReference = references.FirstOrDefault(reference => string.Equals(reference["fieldName"]?.ToString(), "countries", StringComparison.Ordinal))
            ?? throw new AssertFailedException("Fixture did not contain a country reference.");

        return
        [
            new ContentFragmentHtmlEntity(rootPath, (JArray)rootFields.DeepClone(), false),
            CreateReferenceEntity(authorReference),
            CreateReferenceEntity(articleContentReference),
            CreateReferenceEntity(countryReference)
        ];
    }

    private static ContentFragmentHtmlEntity CreateReferenceEntity(JObject reference)
    {
        var path = reference["path"]?.ToString()
            ?? throw new AssertFailedException("Reference did not contain a path.");
        var fields = reference["fields"] as JArray
            ?? throw new AssertFailedException($"Reference '{path}' did not contain fields.");

        return new ContentFragmentHtmlEntity(path, (JArray)fields.DeepClone(), true);
    }

    private JObject LoadFixture()
    {
        var fixturePath = Path.Combine(GetInputDirectory(), FixtureName);
        Assert.IsTrue(File.Exists(fixturePath), $"Fixture file not found at: {fixturePath}");
        return JObject.Parse(File.ReadAllText(fixturePath, Encoding.UTF8));
    }

    private static string GetInputDirectory()
    {
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var projectDirectory = Directory.GetParent(baseDirectory)?.Parent?.Parent?.Parent?.FullName
            ?? throw new DirectoryNotFoundException("Project directory not found.");

        return Path.Combine(projectDirectory, "TestFiles", "Input");
    }
}
