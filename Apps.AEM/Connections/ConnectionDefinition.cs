using Apps.AEM.Constants;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Connections;

namespace Apps.AEM.Connections;

public class ConnectionDefinition : IConnectionDefinition
{
    public IEnumerable<ConnectionPropertyGroup> ConnectionPropertyGroups =>
    [
        new()
        {
            Name = ConnectionTypes.Cloud,
            DisplayName = "Cloud (JSON certificate)",
            AuthenticationType = ConnectionAuthenticationType.Undefined,
            ConnectionProperties =
            [
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
            ]
        },
        new()
        {
            Name = ConnectionTypes.OnPremise,
            DisplayName = "On premise (username and password)",
            AuthenticationType = ConnectionAuthenticationType.Undefined,
            ConnectionProperties =
            [
                new(CredNames.BaseUrl)
                {
                    DisplayName = "Base URL",
                    Description = "Base URL for the AEM instance. Example: https://aem.example.com",
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
            ]
        }
    ];

    public IEnumerable<AuthenticationCredentialsProvider> CreateAuthorizationCredentialsProviders(Dictionary<string, string> values)
    {
        var credentials = values.Select(x => new AuthenticationCredentialsProvider(x.Key, x.Value)).ToList();

        var connectionType = values[nameof(ConnectionPropertyGroup)] switch
        {
            var ct when ConnectionTypes.SupportedConnectionTypes.Contains(ct) => ct,
            _ => throw new Exception($"Unknown connection type: {values[nameof(ConnectionPropertyGroup)]}")
        };
        credentials.Add(new AuthenticationCredentialsProvider(CredNames.ConnectionType, connectionType));

        return credentials;
    }
}
