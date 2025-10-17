namespace Apps.AEM.Constants;

public static class ConnectionTypes
{
    public const string Cloud = "Developer API key"; // name left intact for keeping current connections working
    public const string OnPremise = "On premise (username and password)";

    public static readonly IEnumerable<string> SupportedConnectionTypes = [Cloud, OnPremise];
}
