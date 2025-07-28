using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Apps.AEM.Models.Dtos;

namespace Apps.AEM.Utils.Converters.Tags;

public static class JsonSerializationConvertHelper
{
    public static List<TagNodeDto> ExtractChildTags(JObject jo, JsonSerializer serializer, HashSet<string> skipProperties)
    {
        var children = new List<TagNodeDto>();

        foreach (var prop in jo.Properties())
        {
            // Skip system properties
            if (skipProperties.Contains(prop.Name))
                continue;

            if (prop.Value.Type != JTokenType.Object)
            {
                continue;
            }

            var childObj = (JObject)prop.Value;
            if (childObj == null)
            {
                continue;
            }

            var primaryType = childObj["jcr:primaryType"]?.ToString();
            if (primaryType != "cq:Tag")
            {
                continue;
            }

            var childTag = childObj.ToObject<TagNodeDto>(serializer);
            if (childTag == null)
            {
                continue;
            }

            childTag.TagId = prop.Name; // Set the tag name from the property name
            children.Add(childTag);
        }

        return children;
    }
}