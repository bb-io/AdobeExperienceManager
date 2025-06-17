using Apps.AEM.Models.Entities;
using Apps.AEM.Utils.Converters;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System.Text;
using Tests.AEM.Base;

namespace Tests.AEM;

[TestClass]
public class HtmlToJsonConverterTests : TestBase
{
    private readonly string _testHtml = @"<!DOCTYPE html>
<html><head><meta charset=""UTF-8""><title>Ancient Forest</title><meta name=""blackbird-source-path"" content=""/content/bb-aem-connector/us/en/ancient-forest""></head><body><div data-root=""true"" data-source-path=""/content/bb-aem-connector/us/en/ancient-forest"" data-original-json=""{&quot;jcr:content&quot;:{&quot;jcr:title&quot;:&quot;Ancient Forest&quot;,&quot;root&quot;:{&quot;container&quot;:{&quot;title&quot;:{},&quot;container&quot;:{&quot;text&quot;:{&quot;text&quot;:&quot;&lt;p&gt;The ancient forest is a mysterious and timeless place, home to towering trees that have stood for centuries. Covered in thick moss and echoing with the sounds of wildlife, it offers a glimpse into a world untouched by modern life. Sunlight filters through the dense canopy, casting shifting patterns on the forest floor. Many believe these forests hold secrets of the past, hidden within their roots and shadows. Walking through them feels like stepping into a forgotten legend. Modified at 09.05.2025 12:03&lt;/p&gt;\r\n&quot;}}}}},&quot;references&quot;:[{&quot;referencePath&quot;:&quot;/content/experience-fragments/bb-aem-connector/us/en/site/header/master&quot;},{&quot;referencePath&quot;:&quot;/content/experience-fragments/bb-aem-connector/us/en/site/footer/master&quot;}]}""><span data-json-path=""jcr:content.root.container.container.text.text""><p>The ancient forest is a mysterious and timeless place, home to towering trees that have stood for centuries. Covered in thick moss and echoing with the sounds of wildlife, it offers a glimpse into a world untouched by modern life. Sunlight filters through the dense canopy, casting shifting patterns on the forest floor. Many believe these forests hold secrets of the past, hidden within their roots and shadows. Walking through them feels like stepping into a forgotten legend. Modified at 09.05.2025 12:03</p>
</span></div><div data-reference-path=""/content/experience-fragments/bb-aem-connector/us/en/site/header/master"" data-original-json=""{&quot;jcr:content&quot;:{&quot;jcr:title&quot;:&quot;Header&quot;,&quot;root&quot;:{&quot;navigation&quot;:{},&quot;languagenavigation&quot;:{},&quot;search&quot;:{},&quot;logo&quot;:{&quot;logo.svg&quot;:{&quot;jcr:content&quot;:{}}},&quot;text&quot;:{&quot;text&quot;:&quot;&lt;p&gt;test&lt;/p&gt;\r\n&quot;}}},&quot;references&quot;:[]}""><span data-json-path=""references.jcr:content.jcr:title"">Header</span><span data-json-path=""references.jcr:content.root.text.text""><p>test</p>
</span></div><div data-reference-path=""/content/experience-fragments/bb-aem-connector/us/en/site/footer/master"" data-original-json=""{&quot;jcr:content&quot;:{&quot;jcr:title&quot;:&quot;Footer&quot;,&quot;root&quot;:{&quot;separator&quot;:{},&quot;text&quot;:{&quot;text&quot;:&quot;&lt;p&gt;Copyright 2025, Blackbird AEM Connector.&amp;nbsp;All rights reserved.&lt;/p&gt;\r\n&lt;p&gt;345 Park Avenue,&amp;nbsp;San Jose, CA 95110-2704, USA&lt;/p&gt;\r\n&quot;}}},&quot;references&quot;:[]}""><span data-json-path=""references.jcr:content.jcr:title"">Footer</span><span data-json-path=""references.jcr:content.root.text.text""><p>Copyright 2025, Blackbird AEM Connector.&nbsp;All rights reserved.</p>
<p>345 Park Avenue,&nbsp;San Jose, CA 95110-2704, USA</p>
</span></div></body></html>";

    [TestMethod]
    public async Task ConvertToJson_ValidHtml_ReturnsExpectedEntities()
    {
        // Act
        var entities = HtmlToJsonConverter.ConvertToJson(_testHtml);

        // Assert
        Assert.IsNotNull(entities, "Entities should not be null");
        Assert.IsTrue(entities.Count >= 3, "Should have at least 3 content entities");

        // Find root content entity
        var rootEntity = entities.FirstOrDefault(e => !e.ReferenceContent);
        Assert.IsNotNull(rootEntity, "Root content entity should exist");
        Assert.AreEqual("/content/bb-aem-connector/us/en/ancient-forest", rootEntity.SourcePath, 
            "Root source path should match");
        
        // Check root content
        var rootContent = rootEntity.TargetContent;
        Assert.AreEqual("Ancient Forest", rootContent["jcr:content"]?["jcr:title"]?.ToString(), 
            "Title should be correctly extracted");

        // Verify text content in root
        var textContent = rootContent["jcr:content"]?["root"]?["container"]?["container"]?["text"]?["text"]?.ToString();
        Assert.IsTrue(textContent?.Contains("The ancient forest is a mysterious and timeless place"), 
            "Text content should contain expected paragraph text");
        Assert.IsTrue(textContent?.Contains("Modified at 09.05.2025 12:03"), 
            "Text content should contain the modification timestamp");

        // Check references list in root entity
        Assert.AreEqual(2, rootEntity.References.Count, "Root entity should have 2 references");
        Assert.AreEqual("/content/experience-fragments/bb-aem-connector/us/en/site/header/master", 
            rootEntity.References[0].ReferencePath, "First reference path should match");
        Assert.AreEqual("/content/experience-fragments/bb-aem-connector/us/en/site/footer/master", 
            rootEntity.References[1].ReferencePath, "Second reference path should match");

        // Find reference entities
        var headerEntity = entities.FirstOrDefault(e => e.SourcePath == "/content/experience-fragments/bb-aem-connector/us/en/site/header/master");
        Assert.IsNotNull(headerEntity, "Header entity should exist");
        Assert.IsTrue(headerEntity.ReferenceContent, "Header entity should be marked as reference content");
        Assert.AreEqual("Header", headerEntity.TargetContent["jcr:content"]?["jcr:title"]?.ToString(), 
            "Header title should match");

        var footerEntity = entities.FirstOrDefault(e => e.SourcePath == "/content/experience-fragments/bb-aem-connector/us/en/site/footer/master");
        Assert.IsNotNull(footerEntity, "Footer entity should exist");
        Assert.IsTrue(footerEntity.ReferenceContent, "Footer entity should be marked as reference content");
        Assert.AreEqual("Footer", footerEntity.TargetContent["jcr:content"]?["jcr:title"]?.ToString(), 
            "Footer title should match");

        // Save root JSON to file for inspection
        string json = JsonConvert.SerializeObject(entities, Formatting.Indented);
        var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        memoryStream.Position = 0;
        await FileManager.UploadAsync(memoryStream, "application/json", "converted_root.json");
    }
    
    [TestMethod]
    public void ExtractSourcePath_ValidHtml_ReturnsExpectedPath()
    {
        // Act
        var sourcePath = HtmlToJsonConverter.ExtractSourcePath(_testHtml);
        
        // Assert
        Assert.AreEqual("/content/bb-aem-connector/us/en/ancient-forest", sourcePath, 
            "Source path should be correctly extracted");
    }
}