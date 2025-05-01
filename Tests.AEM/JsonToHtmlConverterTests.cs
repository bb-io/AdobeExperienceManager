using Apps.AEM.Utils.Converters;
using HtmlAgilityPack;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using Tests.AEM.Base;

namespace Tests.AEM;

[TestClass]
public class JsonToHtmlConverterTests : TestBase
{
    private readonly string _testJson = @"{
            ""jcr:content"": {
                ""jcr:title"": ""Clear skies"",
                ""root"": {
                    ""layout"": ""responsiveGrid"",
                    ""container"": {
                        ""layout"": ""responsiveGrid"",
                        ""title"": {},
                        ""container"": {
                            ""layout"": ""responsiveGrid"",
                            ""text"": {
                                ""text"": ""<p>Beneath the vast expanse of a sky unblemished by cloud or mist, the world feels simultaneously limitless and intimate. The horizon, a delicate line where pale cerulean deepens into the richest azure, seems to beckon you forward, as though adventure awaits just beyond your sight. Each breath you draw tastes of clarity itself, free from the weight of storm or shadow</p>\r\n"",
                                ""textIsRich"": ""true""
                            }
                        }
                    }
                }
            }
        }";

    [TestMethod]
    public async Task ConvertToHtml_ValidJson_ReturnsExpectedHtml()
    {
        // Act
        var html = JsonToHtmlConverter.ConvertToHtml(_testJson, "/content/test-page");

        // Assert
        Assert.IsFalse(string.IsNullOrEmpty(html), "HTML should not be empty");
        Assert.IsTrue(html.Contains("Beneath the vast expanse of a sky unblemished by cloud or"), "Rich text content missing");
        
        var memoryStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(html));
        memoryStream.Position = 0;
        await FileManager.UploadAsync(memoryStream, "text/html", "test.html");
    }
}