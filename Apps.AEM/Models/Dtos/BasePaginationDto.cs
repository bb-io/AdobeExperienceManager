using Blackbird.Applications.Sdk.Common;

namespace Apps.AEM.Models.Dtos;
public class BasePaginationDto<T>
{
    [DefinitionIgnore]
    public int Limit { get; set; } = -1;

    [DefinitionIgnore]
    public int Offset { get; set; } = 0;

    [Display("Total count")]
    public double Total { get; set; } = 0;

    [DefinitionIgnore]
    public bool More { get; set; } = false;

    [DefinitionIgnore]
    public int Results { get; set; } = 0;

    public virtual List<T> Content { get; set; } = [];
}
