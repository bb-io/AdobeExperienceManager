namespace Apps.AEM.Constants;

public static class ContentFragmentStatuses
{
    public const string New = "NEW";
    public const string Draft = "DRAFT";
    public const string Published = "PUBLISHED";
    public const string Modified = "MODIFIED";
    public const string Unpublished = "UNPUBLISHED";

    public static readonly string[] All =
    [
        New,
        Draft,
        Published,
        Modified,
        Unpublished
    ];
}
