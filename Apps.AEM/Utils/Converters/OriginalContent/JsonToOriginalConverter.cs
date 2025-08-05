using Newtonsoft.Json.Linq;
using Apps.AEM.Models.Entities;
using Blackbird.Applications.Sdk.Common.Exceptions;

namespace Apps.AEM.Utils.Converters.OriginalContent;

public static class JsonToOriginalConverter
{
    public static IEnumerable<JsonContentEntity> ConvertToEntities(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("Input JSON is null or empty.", nameof(json));

        var root = JObject.Parse(json);
        var entities = new List<JsonContentEntity>();

        // Process root content
        var rootContentPath = root["rootContentPath"]?.ToString() ?? string.Empty;
        var rootContentToken = root["rootContent"] as JObject;
        if (rootContentToken?.Type != JTokenType.Object)
            throw new PluginMisconfigurationException("Main content is not a valid JSON object.");

        var rootReferences = ExtractReferences(rootContentToken);

        var rootContentWithoutReferences = (JObject)rootContentToken.DeepClone();
        rootContentWithoutReferences.Remove("references");
        
        entities.Add(new JsonContentEntity(rootContentPath, rootContentWithoutReferences, rootReferences, false));

        // Process referenced content
        var referencedContent = root["referencedContent"] as JArray ?? [];
        foreach (var refObj in referencedContent)
        {
            var referencePath = refObj["referencePath"]?.ToString() ?? string.Empty;
            var contentToken = refObj["content"] as JObject ?? JObject.Parse(refObj["content"]?.ToString() ?? "{}");
            var references = ExtractReferences(contentToken);

            var contentTokenWithoutReferences = (JObject)contentToken.DeepClone();
            contentTokenWithoutReferences.Remove("references");
            
            entities.Add(new JsonContentEntity(referencePath, contentTokenWithoutReferences, references, true));
        }

        return entities;
    }

    private static List<ReferenceEntity> ExtractReferences(JObject contentToken)
    {
        var referencesArray = contentToken["references"] as JArray;
        return referencesArray?.Select(refObj => new ReferenceEntity(
            refObj["referencePath"]?.ToString() ?? string.Empty,
            refObj["content"]?.ToString() ?? "{}",
            refObj["propertyName"]?.ToString(),
            refObj["propertyPath"]?.ToString()
        )).ToList() ?? [];
    }
}
