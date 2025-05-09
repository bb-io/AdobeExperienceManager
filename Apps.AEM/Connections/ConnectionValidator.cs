using Apps.AEM.Api;
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
            var request = new RestRequest("/content/services/bb-aem-connector/pages/events.json", Method.Get);

            var response = await client.ExecuteWithErrorHandling(request);
            return new()
            {
                IsValid = response.IsSuccessful,
                Message = response.IsSuccessful ? "Connection is valid" : $"Connection failed: {response.Content}",
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
