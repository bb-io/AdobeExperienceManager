using Apps.AEM.Models;
using Blackbird.Applications.Sdk.Common.Authentication;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace Apps.AEM.Utils;

public static class BlackbirdMetadataFactory
{
    private static readonly HashSet<string> KnownLanguageCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "af", "sq", "am", "ar", "hy", "az", "eu", "be", "bn", "bs",
        "bg", "ca", "km", "zh", "hr", "cs", "da", "nl", "en", "et",
        "fi", "fr", "gl", "ka", "de", "el", "gu", "he", "hi", "hu",
        "is", "id", "ga", "it", "ja", "kn", "kk", "ko", "ky", "lo",
        "lv", "lt", "mk", "ms", "ml", "mt", "mi", "mr", "mn", "ne",
        "nb", "nn", "no", "fa", "pl", "pt", "pa", "ro", "ru", "sr",
        "si", "sk", "sl", "es", "sw", "sv", "tl", "ta", "te", "th",
        "tr", "uk", "ur", "uz", "vi", "cy", "zu"
    };

    public static BlackbirdMetadata Create(
        IEnumerable<AuthenticationCredentialsProvider> credentials,
        string contentPath,
        JObject? json = null,
        string? ucidOverride = null,
        string? languageOverride = null,
        string? contentNameOverride = null)
    {
        var baseUrl = credentials.GetBaseUrl();
        return new BlackbirdMetadata
        {
            Ucid = ucidOverride ?? contentPath,
            ContentName = contentNameOverride ?? (json != null ? ParseContentName(json) : null),
            HtmlLanguage = languageOverride ?? ParseLanguageFromPath(contentPath) ?? string.Empty,
            SystemRef = baseUrl
        };
    }

    private static string? ParseLanguageFromPath(string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        foreach (var segment in segments.Skip(1))
        {
            var lower = segment.ToLowerInvariant();
            if (Regex.IsMatch(lower, @"^[a-z]{2}-[a-z]{2}$"))
            {
                var langCode = lower.Split('-')[0];
                if (KnownLanguageCodes.Contains(langCode))
                    return lower;
            }

            if (KnownLanguageCodes.Contains(lower))
            {
                return lower;
            }
        }

        return null;
    }

    private static string? ParseContentName(JObject json)
    {
        if (json.TryGetValue("jcr:content", out var jcrContentToken) && jcrContentToken is JObject jcrContent)
        {
            var jcrTitle = jcrContent["jcr:title"]?.ToString();
            if (!string.IsNullOrWhiteSpace(jcrTitle))
                return jcrTitle;

            var pageTitle = jcrContent["pageTitle"]?.ToString();
            if (!string.IsNullOrWhiteSpace(pageTitle))
                return pageTitle;
        }

        foreach (var key in new[] { "jcr:title", "title", "name" })
        {
            var value = json[key]?.ToString();
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return null;
    }
}
