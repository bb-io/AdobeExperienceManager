using Apps.AEM.Models;
using Blackbird.Filters.Transformations;

namespace Apps.AEM.Utils;

public static class TransformationTargetMetadata
{
    public static void ApplyAemTarget(
        Transformation transformation,
        string originalSourcePath,
        string targetLanguage,
        string systemRef)
    {
        transformation.TargetSystemReference.ContentId = originalSourcePath;
        transformation.TargetSystemReference.SystemName = BlackbirdMetadata.AemSystemName;
        transformation.TargetSystemReference.SystemRef = systemRef;
        transformation.TargetLanguage = targetLanguage;

        var sourceContentName = transformation.SourceSystemReference?.ContentName;
        if (!string.IsNullOrWhiteSpace(sourceContentName))
            transformation.TargetSystemReference.ContentName = sourceContentName;
    }
}
