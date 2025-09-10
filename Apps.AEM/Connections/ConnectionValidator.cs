using Apps.AEM.Api;
using Apps.AEM.Utils;
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
            var request = new RestRequest("/content/services/bb-aem-connector/content/events.json", Method.Get);
            request.AddQueryParameter("startDate", DateTime.UtcNow.ToString(ContentSearch.SearchDateFormat));
            request.AddQueryParameter("limit", 1);

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
