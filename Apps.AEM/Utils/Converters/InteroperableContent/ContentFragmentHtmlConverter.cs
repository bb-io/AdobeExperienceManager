using Apps.AEM.Models;
using Apps.AEM.Models.Entities;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Web;

namespace Apps.AEM.Utils.Converters.InteroperableContent;

public static class ContentFragmentHtmlConverter
{
    private static readonly string[] DefaultExcludedFieldNames = ["history", "previewUrl"];

    public static string ConvertToHtml(JArray fields, string sourcePath, IEnumerable<string>? excludedFields = null, BlackbirdMetadata? metadata = null)
    {
        return ConvertToHtml(
            [new ContentFragmentHtmlEntity(sourcePath, fields, false)],
            excludedFields,
            metadata);
    }

    public static string ConvertToHtml(IEnumerable<ContentFragmentHtmlEntity> entities, IEnumerable<string>? excludedFields = null, BlackbirdMetadata? metadata = null)
    {
        var excludedFieldNames = new HashSet<string>(DefaultExcludedFieldNames, StringComparer.OrdinalIgnoreCase);

        if (excludedFields != null)
        {
            foreach (var excludedField in excludedFields.Where(field => !string.IsNullOrWhiteSpace(field)))
            {
                excludedFieldNames.Add(excludedField.Trim());
            }
        }

        var contentFragmentEntities = entities.ToList();
        var rootEntity = contentFragmentEntities.SingleOrDefault(entity => !entity.ReferenceContent)
            ?? throw new ArgumentException("Exactly one root content fragment entity is required.", nameof(entities));

        var doc = new HtmlDocument();
        var htmlNode = doc.CreateElement("html");

        if (!string.IsNullOrWhiteSpace(metadata?.HtmlLanguage))
            htmlNode.SetAttributeValue("lang", metadata.HtmlLanguage);

        doc.DocumentNode.AppendChild(htmlNode);

        var headNode = doc.CreateElement("head");
        htmlNode.AppendChild(headNode);

        var metaCharset = doc.CreateElement("meta");
        metaCharset.SetAttributeValue("charset", "UTF-8");
        headNode.AppendChild(metaCharset);

        var metaSourcePath = doc.CreateElement("meta");
        metaSourcePath.SetAttributeValue("name", "blackbird-source-path");
        metaSourcePath.SetAttributeValue("content", rootEntity.SourcePath);
        headNode.AppendChild(metaSourcePath);

        AddBlackbirdInteroperabilityMetadata(doc, headNode, metadata);

        var bodyNode = doc.CreateElement("body");
        htmlNode.AppendChild(bodyNode);

        foreach (var entity in contentFragmentEntities)
        {
            var keyPrefix = metadata != null ? entity.SourcePath : (string?)null;
            AppendContentFragment(bodyNode, doc, entity, excludedFieldNames, keyPrefix);
        }

        return "<!DOCTYPE html>\n" + doc.DocumentNode.OuterHtml;
    }

    private static void AddBlackbirdInteroperabilityMetadata(HtmlDocument doc, HtmlNode headNode, BlackbirdMetadata? metadata)
    {
        if (metadata == null)
            return;

        AddMetaTag(doc, headNode, "blackbird-ucid", metadata.Ucid);
        AddMetaTag(doc, headNode, "blackbird-content-name", metadata.ContentName);
        AddMetaTag(doc, headNode, "blackbird-system-name", metadata.SystemName);
        AddMetaTag(doc, headNode, "blackbird-system-ref", metadata.SystemRef);
    }

    private static void AddMetaTag(HtmlDocument doc, HtmlNode headNode, string name, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        var metaNode = doc.CreateElement("meta");
        metaNode.SetAttributeValue("name", name);
        metaNode.SetAttributeValue("content", System.Net.WebUtility.HtmlEncode(value));
        headNode.AppendChild(metaNode);
    }

    private static void AppendContentFragment(
        HtmlNode bodyNode,
        HtmlDocument doc,
        ContentFragmentHtmlEntity entity,
        ISet<string> excludedFieldNames,
        string? keyPrefix)
    {
        var fragmentDivNode = doc.CreateElement("div");

        if (entity.ReferenceContent)
        {
            fragmentDivNode.SetAttributeValue("data-reference-path", entity.SourcePath);
        }
        else
        {
            fragmentDivNode.SetAttributeValue("data-root", "true");
            fragmentDivNode.SetAttributeValue("data-source-path", entity.SourcePath);
        }

        var originalJson = new JObject
        {
            ["fields"] = entity.Fields.DeepClone()
        };

        fragmentDivNode.SetAttributeValue(
            "data-original-json",
            HttpUtility.HtmlEncode(originalJson.ToString(Formatting.None)));

        AppendTranslatableFields(fragmentDivNode, doc, entity.Fields, excludedFieldNames, keyPrefix);
        bodyNode.AppendChild(fragmentDivNode);
    }

    private static void AppendTranslatableFields(
        HtmlNode parentNode,
        HtmlDocument doc,
        JArray fields,
        ISet<string> excludedFieldNames,
        string? keyPrefix)
    {
        for (var fieldIndex = 0; fieldIndex < fields.Count; fieldIndex++)
        {
            if (fields[fieldIndex] is not JObject field)
                continue;

            if (!IsTranslatableField(field))
                continue;

            var fieldName = field["name"]?.ToString();
            if (!string.IsNullOrWhiteSpace(fieldName) && excludedFieldNames.Contains(fieldName))
                continue;

            var values = field["values"] as JArray;
            if (values == null)
                continue;

            for (var valueIndex = 0; valueIndex < values.Count; valueIndex++)
            {
                var value = values[valueIndex];
                if (value == null)
                    continue;

                var node = doc.CreateElement("div");
                node.SetAttributeValue("data-json-path", $"fields[{fieldIndex}].values[{valueIndex}]");
                node.SetAttributeValue("data-field-name", fieldName ?? $"field-{fieldIndex}");

                if (keyPrefix != null && !string.IsNullOrWhiteSpace(fieldName))
                    node.SetAttributeValue("data-blackbird-key", $"{keyPrefix}#{fieldName}[{valueIndex}]");

                var stringValue = value.Type == JTokenType.String
                    ? value.ToString()
                    : value.ToString(Formatting.None);

                if (IsRichTextField(field))
                {
                    node.InnerHtml = stringValue;
                }
                else
                {
                    node.InnerHtml = HttpUtility.HtmlEncode(stringValue);
                }

                parentNode.AppendChild(node);
            }
        }
    }

    private static bool IsTranslatableField(JObject field)
    {
        var fieldType = field["type"]?.ToString();
        return fieldType is "text" or "long-text";
    }

    private static bool IsRichTextField(JObject field)
    {
        return string.Equals(field["type"]?.ToString(), "long-text", StringComparison.OrdinalIgnoreCase)
            && string.Equals(field["mimeType"]?.ToString(), "text/html", StringComparison.OrdinalIgnoreCase);
    }
}
