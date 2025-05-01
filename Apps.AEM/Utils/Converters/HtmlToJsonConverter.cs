using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using System.Web;

namespace Apps.AEM.Utils.Converters;

public static class HtmlToJsonConverter
{
    public static JObject ConvertToJson(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var bodyNode = doc.DocumentNode.SelectSingleNode("//body");
        if (bodyNode == null)
            throw new ArgumentException("HTML does not contain a body element");

        var originalJsonEncoded = bodyNode.GetAttributeValue("data-original-json", null);
        if (string.IsNullOrEmpty(originalJsonEncoded))
        {
            throw new ArgumentException("HTML body does not contain a data-original-json attribute");
        }

        var originalJsonString = HttpUtility.HtmlDecode(originalJsonEncoded);
        var jsonObj = JObject.Parse(originalJsonString);

        var titleNode = doc.DocumentNode.SelectSingleNode("//title");
        if (titleNode != null && !string.IsNullOrEmpty(titleNode.InnerText))
        {
            UpdateJsonValue(jsonObj, "jcr:content.jcr:title", titleNode.InnerText);
        }

        var elementsWithPath = doc.DocumentNode.SelectNodes("//*[@data-json-path]");
        if (elementsWithPath != null)
        {
            foreach (var element in elementsWithPath)
            {
                var jsonPath = element.GetAttributeValue("data-json-path", null);
                if (string.IsNullOrEmpty(jsonPath))
                {
                    continue;
                }

                if (IsRichTextPath(jsonPath) && element.Name != "span")
                {
                    UpdateJsonValue(jsonObj, jsonPath, element.OuterHtml);
                }
                else
                {
                    UpdateJsonValue(jsonObj, jsonPath, element.InnerHtml);
                }
            }
        }

        return jsonObj;
    }

    private static bool IsRichTextPath(string path)
    {
        return path.EndsWith(".text", StringComparison.OrdinalIgnoreCase);
    }

    private static void UpdateJsonValue(JObject jsonObj, string path, string value)
    {
        var pathSegments = SplitJsonPath(path);
        JToken current = jsonObj;

        for (int i = 0; i < pathSegments.Length - 1; i++)
        {
            string segment = pathSegments[i];
            
            if (segment.Contains("[") && segment.EndsWith("]"))
            {
                int indexStart = segment.IndexOf("[");
                string propertyName = segment.Substring(0, indexStart);
                int arrayIndex = int.Parse(segment.Substring(indexStart + 1, segment.Length - indexStart - 2));
                
                if (current![propertyName] == null)
                {
                    current[propertyName] = new JArray();
                }
                
                var array = (JArray)current[propertyName]!;
                while (array.Count <= arrayIndex)
                {
                    array.Add(new JObject());
                }
                
                current = array[arrayIndex];
            }
            else
            {
                if (current![segment] == null)
                    current[segment] = new JObject();
                
                current = current[segment]!;
            }
        }

        string lastSegment = pathSegments[pathSegments.Length - 1];
        
        if (lastSegment.Contains("[") && lastSegment.EndsWith("]"))
        {
            int indexStart = lastSegment.IndexOf("[");
            string propertyName = lastSegment.Substring(0, indexStart);
            int arrayIndex = int.Parse(lastSegment.Substring(indexStart + 1, lastSegment.Length - indexStart - 2));
            
            if (current[propertyName] == null)
            {
                current[propertyName] = new JArray();
            }
            
            var array = (JArray)current[propertyName]!;
            while (array.Count <= arrayIndex)
            {
                array.Add(null!);
            }
            
            array[arrayIndex] = value;
        }
        else
        {
            current[lastSegment] = value;
        }
    }

    private static string[] SplitJsonPath(string path)
    {
        var segments = new List<string>();
        int startIndex = 0;
        bool inBracket = false;
        
        for (int i = 0; i < path.Length; i++)
        {
            char c = path[i];
            
            if (c == '[')
            {
                inBracket = true;
            }
            else if (c == ']')
            {
                inBracket = false;
            }
            else if (c == '.' && !inBracket)
            {
                segments.Add(path.Substring(startIndex, i - startIndex));
                startIndex = i + 1;
            }
        }
        
        segments.Add(path.Substring(startIndex));
        return segments.ToArray();
    }
}
