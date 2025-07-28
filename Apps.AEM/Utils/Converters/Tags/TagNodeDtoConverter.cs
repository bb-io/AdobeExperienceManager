using Apps.AEM.Models.Dtos;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Apps.AEM.Utils.Converters.Tags;

public class TagNodeDtoConverter : JsonConverter
{
    public override bool CanConvert(System.Type objectType) => objectType == typeof(TagNodeDto);

    public override object ReadJson(JsonReader reader, System.Type objectType, object? existingValue, JsonSerializer serializer)
    {
        var jo = JObject.Load(reader);
        var tagNode = new TagNodeDto();
        serializer.Populate(jo.CreateReader(), tagNode);

        tagNode.Tags = JsonSerializationConvertHelper.ExtractChildTags(jo, serializer);
        return tagNode;
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        throw new NotImplementedException("Serialization is not implemented for TagNodeDtoConverter.");
    }
}
