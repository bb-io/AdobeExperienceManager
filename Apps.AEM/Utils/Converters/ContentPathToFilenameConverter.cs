using Blackbird.Applications.Sdk.Common.Exceptions;

namespace Apps.AEM.Utils.Converters;

public class ContentPathToFilenameConverter
{
    public static string PathToFilename(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new PluginApplicationException($"The content path '{path}' is empty.");

        var filename = path.Replace("/", "__");

        var dangerousChars = System.IO.Path.GetInvalidFileNameChars();
        if (filename.IndexOfAny(dangerousChars) >= 0)
        {
            // should never happen, but just in case
            throw new PluginApplicationException($"The content path '{path}' contains unsupported characters for a filename.");
        }

        return filename;
    }
}
