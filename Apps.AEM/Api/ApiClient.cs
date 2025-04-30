using Apps.AEM.Models.Dtos;
using Apps.AEM.Services;
using Apps.AEM.Utils;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Utils.RestSharp;
using Newtonsoft.Json;
using RestSharp;

namespace Apps.AEM.Api;

public class ApiClient(IEnumerable<AuthenticationCredentialsProvider> credentials) : BlackBirdRestClient(new()
    {
        BaseUrl = new Uri(credentials.GetBaseUrl()),
        ThrowOnAnyError = false
    })
{
    public override async Task<RestResponse> ExecuteWithErrorHandling(RestRequest request)
    {
        var token = await TokenService.GetAccessToken(credentials);
        request.AddHeader("Authorization", $"Bearer {token}");
        return await base.ExecuteWithErrorHandling(request);
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
