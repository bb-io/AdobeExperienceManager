using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Apps.AEM.Models.Dtos;

namespace Apps.AEM.Utils.Converters.Tags;

public static class JsonSerializationConvertHelper
{
    public static List<TagNodeDto> ExtractChildTags(JObject jo, JsonSerializer serializer)
    {
        var children = new List<TagNodeDto>();

        foreach (var prop in jo.Properties())
        {
            if (prop.Value.Type != JTokenType.Object)
            {
                continue;
            }

            var tagJObject = (JObject)prop.Value;
            if (tagJObject == null)
            {
                continue;
            }

            var primaryType = tagJObject["jcr:primaryType"]?.ToString();
            if (primaryType != "cq:Tag")
            {
                continue;
            }

            var childTag = tagJObject.ToObject<TagNodeDto>(serializer);
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