namespace Apps.AEM.Models.Dtos;
public class SearchRequestDto
{
    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string? RootPath { get; set; }

    public string? ContentType { get; set; }

    public IEnumerable<string>? Events { get; set; }

    public IEnumerable<string>? Tags { get; set; }

    public string? Keyword { get; set; }

    public int? Offset { get; set; }

    // Limit is influencing only the number of results per page, not the total number of results.
    public int? Limit { get; set; }
}
