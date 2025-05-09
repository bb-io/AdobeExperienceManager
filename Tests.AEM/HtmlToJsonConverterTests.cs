using Apps.AEM.Utils.Converters;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;
using Tests.AEM.Base;

namespace Tests.AEM;

[TestClass]
public class HtmlToJsonConverterTests : TestBase
{
    private readonly string _testHtml = "<!DOCTYPE html> <html><head><meta charset=\"UTF-8\"><title>Ancient Forest</title><meta name=\"blackbird-source-path\" content=\"/content/bb-aem-connector/us/en/ancient-forest\"></head><body data-source-path=\"/content/bb-aem-connector/us/en/ancient-forest\" data-original-json=\"{&quot;jcr:content&quot;:{&quot;jcr:title&quot;:&quot;Ancient Forest&quot;,&quot;root&quot;:{&quot;layout&quot;:&quot;responsiveGrid&quot;,&quot;container&quot;:{&quot;layout&quot;:&quot;responsiveGrid&quot;,&quot;title&quot;:{},&quot;container&quot;:{&quot;layout&quot;:&quot;responsiveGrid&quot;,&quot;text&quot;:{&quot;text&quot;:&quot;&lt;p&gt;The ancient forest is a mysterious and timeless place, home to towering trees that have stood for centuries. Covered in thick moss and echoing with the sounds of wildlife, it offers a glimpse into a world untouched by modern life. Sunlight filters through the dense canopy, casting shifting patterns on the forest floor. Many believe these forests hold secrets of the past, hidden within their roots and shadows. Walking through them feels like stepping into a forgotten legend. Modified at 09.05.2025 10:52&lt;/p&gt;\r\n&quot;,&quot;textIsRich&quot;:&quot;true&quot;}}}}}}\"><p data-json-path=\"jcr:content.root.container.container.text.text\">The ancient forest is a mysterious and timeless place, home to towering trees that have stood for centuries. Covered in thick moss and echoing with the sounds of wildlife, it offers a glimpse into a world untouched by modern life. Sunlight filters through the dense canopy, casting shifting patterns on the forest floor. Many believe these forests hold secrets of the past, hidden within their roots and shadows. Walking through them feels like stepping into a forgotten legend. Modified at 09.05.2025 10:52</p></body></html>";

    [TestMethod]
    public async Task ConvertToJson_ValidHtml_ReturnsExpectedJson()
    {
        // Act
        var jsonObj = HtmlToJsonConverter.ConvertToJson(_testHtml);

        // Assert
        Assert.IsNotNull(jsonObj, "JSON should not be null");
        
        // Verify title
        Assert.AreEqual("Ancient Forest", jsonObj["jcr:content"]?["jcr:title"]?.ToString(), 
            "Title should be correctly extracted");
        
        // Verify the text content
        var textContent = jsonObj["jcr:content"]?["root"]?["container"]?["container"]?["text"]?["text"]?.ToString();
        Assert.IsTrue(textContent?.Contains("The ancient forest is a mysterious and timeless place"), 
            "Text content should contain expected paragraph text");
        Assert.IsTrue(textContent?.Contains("Modified at 09.05.2025 10:52"), 
            "Text content should contain the modification timestamp");

        // Save JSON to file
        string json = jsonObj.ToString(Newtonsoft.Json.Formatting.Indented);
        var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        memoryStream.Position = 0;
        await FileManager.UploadAsync(memoryStream, "application/json", "converted.json");
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