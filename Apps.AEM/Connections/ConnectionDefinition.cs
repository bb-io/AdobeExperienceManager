using Apps.AEM.Constants;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Connections;

namespace Apps.AEM.Connections;

public class ConnectionDefinition : IConnectionDefinition
{
    public IEnumerable<ConnectionPropertyGroup> ConnectionPropertyGroups => new List<ConnectionPropertyGroup>
    {
        new()
        {
            Name = "Developer API key", // cloud, name left intact for keeping current connections working
            DisplayName = "Cloud (JSON certificate)",
            AuthenticationType = ConnectionAuthenticationType.Undefined,
            ConnectionProperties = new List<ConnectionProperty>
            {
                new(CredNames.BaseUrl) 
                { 
                    DisplayName = "Base URL", 
                    Description = "Base URL for the AEM instance. Example: https://author-xxxxx-xxxxx.adobeaemcloud.com",
                    Sensitive = false
                },
                new(CredNames.IntegrationJsonCertificate)
                {
                    DisplayName = "Integration JSON certificate",
                    Description = "Integration certificate in JSON format. Can be found in the Developer Console. Example: { \"ok\": true, \"integration\": { \"imsEndpoint\": \"ims-na1.adobelogin.com\", ... } \"statusCode\": 200}",
                    Sensitive = true
                }
            }
        },
        new()
        {
            Name = "On premise (username and password)",
            DisplayName = "On premise (username and password)",
            AuthenticationType = ConnectionAuthenticationType.Undefined,
            ConnectionProperties = new List<ConnectionProperty>
            {
                new(CredNames.BaseUrl)
                {
                    DisplayName = "Base URL",
                    Description = "Base URL for the AEM instance. Example: https://author-xxxxx-xxxxx.adobeaemcloud.com",
                    Sensitive = false
                },
                new(CredNames.Username)
                {
                    DisplayName = "Username",
                    Sensitive = false
                },
                new(CredNames.Password)
                {
                    DisplayName = "Password",
                    Sensitive = true
                }
            }
        }
    };

    public IEnumerable<AuthenticationCredentialsProvider> CreateAuthorizationCredentialsProviders(Dictionary<string, string> values)
    {
        var credentials = values.Select(x => new AuthenticationCredentialsProvider(x.Key, x.Value)).ToList();

        var connectionType = values[nameof(ConnectionPropertyGroup)] switch
        {
            "On premise (username and password)" => "on_premise",
            "Developer API key" => "cloud",
            _ => throw new Exception($"Unknown connection type: {values[nameof(ConnectionPropertyGroup)]}")
        };

        if (values[nameof(ConnectionPropertyGroup)] == "Developer API key")
            credentials.Add(new AuthenticationCredentialsProvider(CredNames.ConnectionType, connectionType));

        return credentials;
    }
}
