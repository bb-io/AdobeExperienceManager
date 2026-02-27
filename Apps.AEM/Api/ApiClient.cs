using Apps.AEM.Constants;
using Apps.AEM.Models.Dtos;
using Apps.AEM.Services;
using Apps.AEM.Utils;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Utils.RestSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
    /// <summary>
    /// Requests content data for multiple paths and extracts a mapping of Path -> Tags.
    /// </summary>
    /// <param name="paths">One or more AEM content or DAM paths (e.g., "/content/wknd/jcr:content").</param>
    /// <returns>A dictionary where keys are paths and values are lists of tags.</returns>
    public async Task<Dictionary<string, List<string>>> GetContentTagsAsync(IEnumerable<string> paths)
    {
        var finalResult = new Dictionary<string, List<string>>();

        if (paths == null || !paths.Any())
            return finalResult;

        var sitesPaths = paths.Where(p => p.StartsWith("/content/") && !p.StartsWith("/content/dam/")).ToList();
        var damPaths = paths.Where(p => p.StartsWith("/content/dam/")).ToList();

        var tasks = new List<Task<Dictionary<string, List<string>>>>();

        // Sites can be batched in a single call
        if (sitesPaths.Count > 0)
            tasks.Add(GetSitesTagsAsync(sitesPaths));

        // DAM assets are fetched individually
        foreach (var damPath in damPaths)
            tasks.Add(GetDamAssetTagsAsync(damPath));

        var results = await Task.WhenAll(tasks);

        foreach (var dict in results)
            foreach (var entry in dict)
                finalResult[entry.Key] = entry.Value;

        return finalResult;
    }

    public async Task<Dictionary<string, List<string>>> GetSitesTagsAsync(IEnumerable<string> paths)
    {
        var request = new RestRequest("/content/services/bb-aem-connector/content.json");

        foreach (var path in paths)
        {
            request.AddQueryParameter("contentPath", path);
        }

        var response = await ExecuteWithErrorHandling(request);
        var dict = new Dictionary<string, List<string>>();

        if (string.IsNullOrWhiteSpace(response.Content))
            return dict;

        var json = JObject.Parse(response.Content);

        if (json["values"] is not JArray contentItems)
            return dict;

        foreach (var item in contentItems)
        {
            var pathKey = item["path"]?.ToString().Replace("/jcr:content", "");

            if (string.IsNullOrEmpty(pathKey))
                continue;

            var tags = item["properties"]?
                .FirstOrDefault(p => p["name"]?.ToString() == "cq:tags")?["values"]?
                .ToObject<List<string>>() ?? [];

            dict[pathKey] = tags;
        }

        return dict;
    }

    public async Task<Dictionary<string, List<string>>> GetDamAssetTagsAsync(string damPath)
    {
        var result = new Dictionary<string, List<string>>();

        if (string.IsNullOrWhiteSpace(damPath))
            return result;

        string apiPath = damPath.Replace("/content/dam/", "/api/assets/", StringComparison.OrdinalIgnoreCase);

        var request = new RestRequest($"{apiPath}.json", Method.Get);
        var response = await ExecuteWithErrorHandling(request);

        if (string.IsNullOrWhiteSpace(response.Content))
            return result;

        var json = JObject.Parse(response.Content);
        var tags = json["properties"]?["metadata"]?["cq:tags"]?
            .ToObject<List<string>>() ?? [];

        result[damPath] = tags;

        return result;
    }

    public override async Task<RestResponse> ExecuteWithErrorHandling(RestRequest request)
    {
        request.AddHeader("Cache-Control", "no-cache");

        if (credentials.GetConnectionType() == ConnectionTypes.Cloud)
        {
            var token = await TokenService.GetAccessTokenAsync(credentials);
            request.AddHeader("Authorization", $"Bearer {token}");
        }

        // TODO Handle errors like "Forbidded" more explicitly
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
