using Apps.AEM.Api;
using Apps.AEM.Models.ApiPayloads;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Connections;
using RestSharp;

namespace Apps.AEM.Connections;

public class ConnectionValidator: IConnectionValidator
{
    public async ValueTask<ConnectionValidationResponse> ValidateConnection(
        IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = new ApiClient(authenticationCredentialsProviders);

            var request = new RestRequest("/bin/querybuilder.json")
                .AddQueryParameter("path", "/content/")
                .AddQueryParameter("p.guessTotal", "true")
                .AddQueryParameter("p.limit", "1");

            var response = await client.ExecuteWithErrorHandling<QueryBuilderDto>(request);

            return new()
            {
                IsValid = response.Success,
                Message = response.Success
                    ? "Connection is valid"
                    : "Connection failed: Query Builder response did not return success=true",
            };
        } catch(Exception ex)
        {
            return new()
            {
                IsValid = false,
                Message = ex.Message
            };
        }
    }
}
