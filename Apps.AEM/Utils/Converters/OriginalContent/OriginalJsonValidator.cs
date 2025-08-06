using Newtonsoft.Json;

namespace Apps.AEM.Utils.Converters.OriginalContent;

public class OriginalJsonValidator
{
    public static bool IsJson(string jsonString)
    {
        if (string.IsNullOrWhiteSpace(jsonString))
            return false;

        using var stringReader = new StringReader(jsonString);
        using var jsonReader = new JsonTextReader(stringReader);
        try
        {
            while (jsonReader.Read()) { }
            return true;
        }
        catch (JsonReaderException)
        {
            return false;
        }
    }
}
