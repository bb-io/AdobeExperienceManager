namespace Apps.AEM.Models;

public class BlackbirdMetadata
{
    public string HtmlLanguage { get; init; } = string.Empty;

    public string Ucid { get; init; } = string.Empty;

    public string? ContentName { get; init; }

    public const string AemSystemName = "AEM (Adobe Experience Manager)";

    public string SystemName => AemSystemName;

    public string SystemRef { get; init; } = string.Empty;
}