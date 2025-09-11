using Apps.AEM.Api;
using Apps.AEM.Utils;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Connections;

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

            var request = ContentSearch.BuildRequest(new()
            {
                StartDate = DateTime.UtcNow.AddMinutes(-5),
                Limit = 1
            });

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
