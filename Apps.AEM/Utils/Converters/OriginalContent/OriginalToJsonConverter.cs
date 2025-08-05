using Apps.AEM.Models.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Apps.AEM.Utils.Converters.OriginalContent;

public static class OriginalToJsonConverter
{
    public static string ConvertToJson(
        string rootContentJson,
        string rootContentPath,
        IEnumerable<ReferenceEntity> referencedContent,
        bool isIncludeReferencesEnabled = false)
    {
        return JsonConvert.SerializeObject(new
        {
            rootContentPath,
            isIncludeReferencesEnabled,
            rootContent = JRaw.Parse(rootContentJson),
            referencedContent = referencedContent.Select(reference =>
            {
                return new
                {
                    referencePath = reference.ReferencePath,
                    propertyName = reference.PropertyName,
                    propertyPath = reference.PropertyPath,
                    content = JRaw.Parse(reference.Content),
                };
            })
        });
    }
}
