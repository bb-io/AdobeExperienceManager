using Apps.AEM.Models.Dtos;
using Apps.AEM.Models.Entities;
using Blackbird.Applications.Sdk.Common.Exceptions;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Web;

namespace Apps.AEM.Utils.Converters.InteroperableContent;

public static class JsonToHtmlConverter
{
    public static IEnumerable<ReferenceDto> ExtractReferences(string json)
    {
        var jsonObj = JsonConvert.DeserializeObject<JObject>(json);
        if (jsonObj == null)
        {
            return [];
        }

        var references = new List<ReferenceDto>();
        var referencesToken = jsonObj["references"];

        if (referencesToken != null && referencesToken.Type == JTokenType.Array)
        {
            var referencesArray = (JArray)referencesToken;
            foreach (var reference in referencesArray)
            {
                if (reference.Type == JTokenType.Object)
                {
                    var referenceObj = (JObject)reference;
                    var referenceDto = new ReferenceDto
                    {
                        PropertyName = referenceObj["propertyName"]?.ToString(),
                        PropertyPath = referenceObj["propertyPath"]?.ToString(),
                        ReferencePath = referenceObj["referencePath"]?.ToString() ?? string.Empty
                    };
                    references.Add(referenceDto);
                }
            }
        }

        return references;
    }

    public static string ConvertToHtml(string json, string sourcePath, IEnumerable<ReferenceEntity> referenceEntities)
    {
        var jsonObj = JsonConvert.DeserializeObject<JObject>(json)
            ?? throw new PluginApplicationException("Received an empty intut for HTML conversion");

        var doc = new HtmlDocument();
        var htmlNode = doc.CreateElement("html");
        doc.DocumentNode.AppendChild(htmlNode);

        var headNode = doc.CreateElement("head");
        htmlNode.AppendChild(headNode);

        var metaCharset = doc.CreateElement("meta");
        metaCharset.SetAttributeValue("charset", "UTF-8");
        headNode.AppendChild(metaCharset);

        var metaSourcePath = doc.CreateElement("meta");
        metaSourcePath.SetAttributeValue("name", "blackbird-source-path");
        metaSourcePath.SetAttributeValue("content", sourcePath);
        headNode.AppendChild(metaSourcePath);

        var bodyNode = doc.CreateElement("body");
        htmlNode.AppendChild(bodyNode);

        var rootDivNode = doc.CreateElement("div");
        rootDivNode.SetAttributeValue("data-root", "true");
        rootDivNode.SetAttributeValue("data-source-path", sourcePath);
        rootDivNode.SetAttributeValue("data-original-json", HttpUtility.HtmlEncode(jsonObj.ToString(Formatting.None)));

        ProcessJsonContent(jsonObj, rootDivNode, doc, "");
        bodyNode.AppendChild(rootDivNode);

        foreach (var referenceEntity in referenceEntities)
        {
            AppendReferenceContent(bodyNode, doc, referenceEntity);
        }

        return "<!DOCTYPE html>\n" + doc.DocumentNode.OuterHtml;
    }

    private static void AppendReferenceContent(HtmlNode parentNode, HtmlDocument doc, ReferenceEntity referenceEntity)
    {
        var referenceDiv = doc.CreateElement("div");
        referenceDiv.SetAttributeValue("data-reference-path", referenceEntity.ReferencePath);
        referenceDiv.SetAttributeValue("data-original-json", HttpUtility.HtmlEncode(referenceEntity.Content));
        var jObject = JsonConvert.DeserializeObject<JObject>(referenceEntity.Content)!;
        ProcessJsonContent(jObject, referenceDiv, doc, "references");
        parentNode.AppendChild(referenceDiv);
    }

    private static void ProcessJsonContent(JObject jsonObj, HtmlNode parentNode, HtmlDocument doc, string jsonPath)
    {
        foreach (var property in jsonObj)
        {
            // Skip the references array to prevent adding reference metadata to the visible content
            if (property.Key == "references")
                continue;
                
            string currentPath = AppendJsonPath(jsonPath, property.Key);
            if (property.Value?.Type == JTokenType.Object)
            {
                var jObj = (JObject)property.Value;
                if (jObj["text"] != null && jObj["textIsRich"] != null &&
                    string.Equals(jObj["textIsRich"]?.ToString(), "true", StringComparison.OrdinalIgnoreCase))
                {
                    ProcessRichText(jObj["text"]!.ToString(), parentNode, doc, currentPath + ".text");
                }
                else
                {
                    ProcessJsonContent(jObj, parentNode, doc, currentPath);
                }
            }
            else if (property.Value?.Type == JTokenType.Array)
            {
                ProcessJsonArray((JArray)property.Value, parentNode, doc, currentPath);
            }
            else if (property.Value?.Type != JTokenType.Null)
            {
                if (!currentPath.EndsWith(".layout"))
                {
                    var textNode = doc.CreateElement("span");
                    textNode.SetAttributeValue("data-json-path", currentPath);
                    textNode.InnerHtml = property.Value?.ToString() ?? string.Empty;
                    parentNode.AppendChild(textNode);
                }
            }
        }
    }

    private static void ProcessJsonArray(JArray array, HtmlNode parentNode, HtmlDocument doc, string jsonPath)
    {
        // Skip processing if this is the references array
        if (jsonPath.Equals("references"))
            return;
            
        for (int i = 0; i < array.Count; i++)
        {
            var item = array[i];
            string itemPath = jsonPath + "[" + i + "]";

            if (item.Type == JTokenType.Object)
            {
                var container = doc.CreateElement("div");
                container.SetAttributeValue("data-json-path", itemPath);
                parentNode.AppendChild(container);

                ProcessJsonContent((JObject)item, container, doc, itemPath);
            }
            else if (item.Type == JTokenType.Array)
            {
                ProcessJsonArray((JArray)item, parentNode, doc, itemPath);
            }
            else if (item.Type != JTokenType.Null)
            {
                var textNode = doc.CreateElement("span");
                textNode.SetAttributeValue("data-json-path", itemPath);
                textNode.InnerHtml = item.ToString();
                parentNode.AppendChild(textNode);
            }
        }
    }

    private static void ProcessRichText(string htmlContent, HtmlNode parentNode, HtmlDocument doc, string jsonPath)
    {
        var tempDoc = new HtmlDocument();
        tempDoc.LoadHtml(htmlContent);

        foreach (var node in tempDoc.DocumentNode.ChildNodes)
        {
            if (node.NodeType == HtmlNodeType.Element)
            {
                var newNode = doc.CreateElement(node.Name);

                foreach (var attribute in node.Attributes)
                {
                    newNode.SetAttributeValue(attribute.Name, attribute.Value);
                }

                newNode.SetAttributeValue("data-json-path", jsonPath);

                newNode.InnerHtml = node.InnerHtml;
                parentNode.AppendChild(newNode);
            }
        }
    }

    private static string AppendJsonPath(string basePath, string propertyName)
    {
        return string.IsNullOrEmpty(basePath) ? propertyName : basePath + "." + propertyName;
    }
}