using Apps.AEM.Utils;
using Apps.AEM.Utils.Converters.InteroperableContent;
using Blackbird.Filters.Bilingual.Xliff2;
using HtmlAgilityPack;

namespace Tests.AEM;

[TestClass]
public class XliffContentConverterTests
{
    [TestMethod]
    [DataRow("<html><body>Article</body></html>", "article.html")]
    [DataRow("{\"title\":\"Article\"}", "article.json")]
    [DataRow("<topic id=\"article\"><title>Article</title></topic>", "article.dita")]
    public void ToTarget_NonXliffPreservesOriginalContent(string content, string fileName)
    {
        var result = XliffContentConverter.ToTarget(content, fileName);

        Assert.AreEqual(content, result.Content);
        Assert.IsNull(result.Transformation);
    }

    [TestMethod]
    public void ToTarget_TranslatedArticlePreservesTargetTextAndAemPayload()
    {
        const string fileName = "__content__wknd__us__en__magazine__san-diego-surf.html-en-ko-T-C.xlf";
        var fixture = Path.Combine(AppContext.BaseDirectory, "../../../TestFiles/Input", fileName);
        var result = XliffContentConverter.ToTarget(File.ReadAllText(fixture), fileName);

        var document = new HtmlDocument();
        document.LoadHtml(result.Content);
        var title = document.DocumentNode.SelectSingleNode("/html/body/div[1]/div[1]");
        Assert.IsNotNull(title);
        Assert.AreEqual("샌디에이고 서핑 명소", title.InnerText);
        var entities = HtmlToJsonConverter.ConvertToJson(result.Content).ToList();
        Assert.IsTrue(entities.Any(entity => entity.SourcePath == "/content/wknd/us/en/magazine/san-diego-surf"));
        Assert.IsNotNull(result.Transformation);
        Assert.AreEqual(fileName, result.Transformation.BilingualFileName);

        TransformationTargetMetadata.ApplyAemTarget(result.Transformation,
            "/content/wknd/us/en/magazine/san-diego-surf", "ko", "https://author.example.com");
        var saved = Xliff2Serializer.Deserialize(result.Transformation.Serialize());
        Assert.AreEqual("ko", saved.TargetLanguage);
        Assert.AreEqual("/content/wknd/us/en/magazine/san-diego-surf", saved.TargetSystemReference.ContentId);
        Assert.AreEqual("https://author.example.com", saved.TargetSystemReference.SystemRef);
    }

    [TestMethod]
    public void ToTarget_FragmentSkeletonPreservesArticleFields()
    {
        const string fileName = "content-fragment-article-translated.xlf";
        var fixture = Path.Combine(AppContext.BaseDirectory, "../../../TestFiles/Input", fileName);
        var result = XliffContentConverter.ToTarget(File.ReadAllText(fixture), fileName);

        var document = new HtmlDocument();
        document.LoadHtml(result.Content);
        var title = document.DocumentNode.SelectSingleNode("//*[@data-field-name='title']");
        Assert.IsNotNull(title);
        Assert.AreEqual("[BB XLIFF] More UK pork producers cleared to restart exports to China", title.InnerText);
        Assert.AreEqual(1, HtmlToJsonConverter.ConvertToJson(result.Content).Count());
    }
}
