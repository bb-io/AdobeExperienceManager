using System.Xml.Serialization;

namespace Apps.AEM.Models.Dtos;

[XmlRoot("response")]
public class UploadDitaContentResponseDto
{
    [XmlElement("message")]
    public string Message { get; set; } = string.Empty;

    [XmlElement("path")]
    public string Path { get; set; } = string.Empty;
}
