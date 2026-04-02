using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Apps.AEM.Models.Dtos;

public class CursorPaginationDto<T>
{
    [JsonProperty("items")]
    public List<T> Items { get; set; } = [];

    [JsonProperty("cursor")]
    public string? Cursor { get; set; }
}

public class ContentFragmentDto
{
    [JsonProperty("path")]
    public string Path { get; set; } = string.Empty;

    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;

    [JsonProperty("description")]
    public string Description { get; set; } = string.Empty;

    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("status")]
    public string Status { get; set; } = string.Empty;

    [JsonProperty("created")]
    public ContentFragmentAuthoringInfoDto? Created { get; set; }

    [JsonProperty("modified")]
    public ContentFragmentAuthoringInfoDto? Modified { get; set; }

    [JsonProperty("fields")]
    public JArray Fields { get; set; } = new();

    [JsonProperty("variations")]
    public List<ContentFragmentVariationDto> Variations { get; set; } = [];

    [JsonProperty("tags")]
    public JArray Tags { get; set; } = new();

    [JsonProperty("fieldTags")]
    public JArray FieldTags { get; set; } = new();

    [JsonProperty("etag")]
    public string? Etag { get; set; }

    [JsonProperty("model")]
    public ContentFragmentModelIdentifierDto? Model { get; set; }
}

public class ContentFragmentVariationDto
{
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;

    [JsonProperty("description")]
    public string Description { get; set; } = string.Empty;

    [JsonProperty("fields")]
    public JArray Fields { get; set; } = new();

    [JsonProperty("created")]
    public ContentFragmentAuthoringInfoDto? Created { get; set; }

    [JsonProperty("modified")]
    public ContentFragmentAuthoringInfoDto? Modified { get; set; }
}

public class ContentFragmentPermissionsDto
{
    [JsonProperty("items")]
    public List<string> Items { get; set; } = [];
}

public class ContentFragmentAuthoringInfoDto
{
    [JsonProperty("at")]
    public DateTime? At { get; set; }

    [JsonProperty("by")]
    public string? By { get; set; }
}

public class ContentFragmentModelIdentifierDto
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("path")]
    public string Path { get; set; } = string.Empty;

    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;
}
