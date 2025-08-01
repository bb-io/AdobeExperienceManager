﻿using Apps.AEM.Models.Dtos;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Apps.AEM.Utils.Converters.Tags;

public class TagsApiPayloadDtoConverter : JsonConverter
{
    public override bool CanConvert(System.Type objectType) => objectType == typeof(TagsApiPayloadDto);

    public override object ReadJson(JsonReader reader, System.Type objectType, object? existingValue, JsonSerializer serializer)
    {
        var jo = JObject.Load(reader);
        var payload = new TagsApiPayloadDto();
        serializer.Populate(jo.CreateReader(), payload);

        payload.Tags = JsonSerializationConvertHelper.ExtractChildTags(jo, serializer);
        return payload;
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        throw new NotImplementedException("Serialization is not implemented for TagsApiPayloadDtoConverter.");
    }
}
