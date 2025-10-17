using Apps.AEM.Constants;
using Apps.AEM.Models.Dtos;
using Apps.AEM.Services;
using Apps.AEM.Utils;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Utils.RestSharp;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators;

namespace Apps.AEM.Api;

public class ApiClient(IEnumerable<AuthenticationCredentialsProvider> credentials) : BlackBirdRestClient(new()
    {
        BaseUrl = new Uri(credentials.GetBaseUrl()),
        ThrowOnAnyError = false,
        Authenticator = credentials.GetConnectionType() == ConnectionTypes.Cloud
            ? null
            : new HttpBasicAuthenticator(credentials.GetUsername(), credentials.GetPassword())
})
{
    public override async Task<RestResponse> ExecuteWithErrorHandling(RestRequest request)
    {
        request.AddHeader("Cache-Control", "no-cache");

        if (credentials.GetConnectionType() == ConnectionTypes.Cloud)
        {
            var token = await TokenService.GetAccessTokenAsync(credentials);
            request.AddHeader("Authorization", $"Bearer {token}");
        }

        var response = await base.ExecuteWithErrorHandling(request);
        if(response.ContentType == "text/html")
        {
            throw new PluginApplicationException($"We got an unexpected HTML response from the server. Please, verify that your AEM instance is up and running (not hibernated)");
        }

        return response;
    }

    public async Task<List<T>> Paginate<T>(RestRequest request, int maxResultsToFetch = 1000)
    {
        var result = new List<T>();
        var offset = 0;
        var limit = 50;
        
        var limitParameter = request.Parameters.FirstOrDefault(p => p.Name?.ToString().Equals("limit", StringComparison.OrdinalIgnoreCase) == true);
        if (limitParameter != null && limitParameter.Value != null)
        {
            limit = Convert.ToInt32(limitParameter.Value);
        }
        
        bool hasMore;
        do
        {
            var offsetParam = request.Parameters.FirstOrDefault(p => p.Name?.ToString().Equals("offset", StringComparison.OrdinalIgnoreCase) == true);
            if (offsetParam != null)
            {
                request.Parameters.RemoveParameter(offsetParam);
            }

            request.AddQueryParameter("offset", offset);
            var response = await ExecuteWithErrorHandling<BasePaginationDto<T>>(request);
            if (response.Content != null)
            {
                result.AddRange(response.Content);
            }

            if (result.Count >= maxResultsToFetch)
            {
                break;
            }

            hasMore = response.More;
            offset += limit;
            
        } while (hasMore);
        
        return result;
    }

    protected override Exception ConfigureErrorException(RestResponse response)
    {
        if(string.IsNullOrEmpty(response.Content))
        {
            if(string.IsNullOrEmpty(response.ErrorMessage))
            {
                throw new PluginApplicationException($"Error while executing request. Status code: {response.StatusCode}; Description: {response.StatusDescription}");
            }

            throw new PluginApplicationException(response.ErrorMessage);
        }

        try 
        {
            var errorDto = JsonConvert.DeserializeObject<ErrorDto>(response.Content);
            
            if (errorDto != null)
            {
                var errorMessage = !string.IsNullOrEmpty(errorDto.Message) 
                    ? errorDto.Message 
                    : errorDto.Error;
                    
                return new PluginApplicationException(
                    $"{errorMessage} (Status code: {errorDto.Status}, Path: {errorDto.Path})");
            }
        }
        catch
        { }
        
        return new PluginApplicationException(response.Content);
    }
}
