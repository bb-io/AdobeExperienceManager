namespace Apps.AEM.Models.Dtos;

public record DitaNodeDataDto(
    string Path,
    IEnumerable<string> Tags
);
