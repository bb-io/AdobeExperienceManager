using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Web;

namespace Apps.AEM.Utils.Converters;

public static class JsonToHtmlConverter
{
    public static string ExtractTitle(string json)
    {
        var jsonObj = JsonConvert.DeserializeObject<JObject>(json);
        return ExtractTitle(jsonObj!);
    }

    public static string ConvertToHtml(string json, string sourcePath)
    {
        var jsonObj = JsonConvert.DeserializeObject<JObject>(json)!;
        
        var doc = new HtmlDocument();
        var htmlNode = doc.CreateElement("html");
        doc.DocumentNode.AppendChild(htmlNode);
        
        var headNode = doc.CreateElement("head");
        htmlNode.AppendChild(headNode);
        
        var metaCharset = doc.CreateElement("meta");
        metaCharset.SetAttributeValue("charset", "UTF-8");
        headNode.AppendChild(metaCharset);
        
        var titleNode = doc.CreateElement("title");
        string title = ExtractTitle(jsonObj);
        titleNode.InnerHtml = title;
        headNode.AppendChild(titleNode);
        
        var metaSourcePath = doc.CreateElement("meta");
        metaSourcePath.SetAttributeValue("name", "blackbird-source-path");
        metaSourcePath.SetAttributeValue("content", sourcePath);
        headNode.AppendChild(metaSourcePath);
        
        var bodyNode = doc.CreateElement("body");
        bodyNode.SetAttributeValue("data-source-path", sourcePath);
        bodyNode.SetAttributeValue("data-original-json", HttpUtility.HtmlEncode(jsonObj.ToString(Formatting.None)));
        htmlNode.AppendChild(bodyNode);
        
        ProcessJsonContent(jsonObj, bodyNode, doc, "");
        
        return "<!DOCTYPE html>\n" + doc.DocumentNode.OuterHtml;
    }
    
    private static string ExtractTitle(JObject jsonObj)
    {
        if (jsonObj["jcr:content"] != null && jsonObj["jcr:content"]?["jcr:title"] != null)
        {
            return jsonObj["jcr:content"]!["jcr:title"]!.ToString();
        }
        
        return "Untitled";
    }
    
    private static void ProcessJsonContent(JObject jsonObj, HtmlNode parentNode, HtmlDocument doc, string jsonPath)
    {
        foreach (var property in jsonObj)
        {
            string currentPath = AppendJsonPath(jsonPath, property.Key);
            
            if (currentPath == "jcr:content.jcr:title")
            {
                continue;
            }
            
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
                    textNode.InnerHtml = property.Value?.ToString();
                    parentNode.AppendChild(textNode);
                }
            }
        }
    }
    
    private static void ProcessJsonArray(JArray array, HtmlNode parentNode, HtmlDocument doc, string jsonPath)
    {
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