using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Filters.Bilingual.Xliff2;
using Blackbird.Filters.Transformations;
using System.Text;

namespace Apps.AEM.Utils.Converters.InteroperableContent;

public static class XliffContentConverter
{
    public static (string Content, Transformation? Transformation) ToTarget(string content, string fileName)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        if (!Xliff2Serializer.IsXliff2(stream, out var xliff))
            return (content, null);

        var transformation = Xliff2Serializer.Deserialize(xliff);
        transformation.BilingualFileName = fileName;
        var target = transformation.Target();
        if (!target.Success)
            throw new PluginMisconfigurationException($"Could not read XLIFF target content: {target.Error}");

        using var targetStream = target.Value.ToStream();
        using var reader = new StreamReader(targetStream, Encoding.UTF8);
        return (reader.ReadToEnd(), transformation);
    }
}
