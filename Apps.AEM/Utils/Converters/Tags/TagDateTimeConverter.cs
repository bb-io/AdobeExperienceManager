using Newtonsoft.Json.Converters;

namespace Apps.AEM.Utils.Converters.Tags;

public class TagDateTimeConverter : IsoDateTimeConverter
{
    public TagDateTimeConverter()
    {
        base.DateTimeFormat = "ddd MMM dd yyyy HH:mm:ss 'GMT'K";
    }
}
