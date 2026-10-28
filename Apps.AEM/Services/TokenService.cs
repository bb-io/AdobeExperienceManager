using Apps.AEM.Utils;
using Blackbird.Applications.Sdk.Common.Authentication;
using RestSharp;
using System.Security.Cryptography;
using Apps.AEM.Models.Dtos;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Text;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Polly;
using Polly.Retry;

namespace Apps.AEM.Services;

public static class TokenService
{
    private static readonly ConcurrentDictionary<string, TokenCacheItem> AccessTokenCache = new();
    private static readonly ConcurrentDictionary<string, RSAWrapper> RsaCache = new();
    private static readonly TimeSpan DefaultTokenLifetime = TimeSpan.FromMinutes(55);
    
    private static readonly ResiliencePipeline<RestResponse> ImsRetryPipeline = new ResiliencePipelineBuilder<RestResponse>()
        .AddRetry(new RetryStrategyOptions<RestResponse>
        {
            MaxRetryAttempts = 2,
            Delay = TimeSpan.FromSeconds(5),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            ShouldHandle = new PredicateBuilder<RestResponse>()
                .HandleResult(r => r.ResponseStatus is ResponseStatus.TimedOut or ResponseStatus.Error || (int)r.StatusCode >= 500)
        })
        .Build();

    private class TokenCacheItem
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }

        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    }

    private class RSAWrapper : IDisposable
    {
        public RSA Rsa { get; private set; }
        private bool _disposed = false;

        public RSAWrapper(string pemKey)
        {
            Rsa = RSA.Create();
            Rsa.ImportFromPem(pemKey.ToCharArray());
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Rsa.Dispose();
                _disposed = true;
            }
        }
    }

    public static async Task<string> GetAccessTokenAsync(IEnumerable<AuthenticationCredentialsProvider> credentials)
    {
        var technicalAccount = credentials.GetTechnicalAccount();
        string cacheKey = technicalAccount.ClientId;

        if (AccessTokenCache.TryGetValue(cacheKey, out var cacheItem) && !cacheItem.IsExpired)
        {
            return cacheItem.Token;
        }
        
        using var client = new RestClient(new RestClientOptions($"https://{credentials.GetImsEndpoint()}")
        {
            MaxTimeout = 20_000 //  ms; the JWT dies after 30s anyway
        });
        
        var response = await ImsRetryPipeline.ExecuteAsync(async ct =>
        {
            var tokenRequest = new RestRequest("/ims/exchange/jwt", Method.Post) { AlwaysMultipartFormData = true }
                .AddHeader("Cache-Control", "no-cache")
                .AddParameter("client_id", technicalAccount.ClientId)
                .AddParameter("client_secret", technicalAccount.ClientSecret)
                .AddParameter("jwt_token", GetJwtToken(credentials));

            return await client.ExecuteAsync(tokenRequest, ct);
        });
        
        if (!response.IsSuccessful)
            throw new PluginApplicationException($"Failed to get access token: {response.ErrorMessage} - {response.Content}");

        var accessToken = DeserializeAccessToken(response.Content!);

        // Cache the token
        AccessTokenCache[cacheKey] = new TokenCacheItem
        {
            Token = accessToken,
            ExpiresAt = DateTime.UtcNow.Add(DefaultTokenLifetime)
        };

        return accessToken;
    }

    public static string GetJwtToken(IEnumerable<AuthenticationCredentialsProvider> credentials)
    {
        var technicalAccount = credentials.GetTechnicalAccount();
        var jsonCertificate = credentials.GetJsonCertificate();

        var authCertificateDto = JsonConvert.DeserializeObject<AuthCertificateDto>(jsonCertificate)
                                 ?? throw new ArgumentNullException(
                                     $"Integration JSON certificate is not valid: {jsonCertificate}");

        var integration = authCertificateDto.Integration ??
                          throw new ArgumentNullException(
                              $"Integration JSON certificate is not valid: {jsonCertificate}");

        var privateKeyPem = (integration.PrivateKey ?? string.Empty)
            .Replace("\\r\\n", "\n")
            .Replace("\\n", "\n");

        string rsaCacheKey = technicalAccount.ClientId;
        var rsaWrapper = RsaCache.GetOrAdd(rsaCacheKey, _ => new RSAWrapper(privateKeyPem));
        using var sha256 = SHA256.Create();

        // Заголовок JWT
        var header = new
        {
            alg = "RS256",
            typ = "JWT"
        };

        var now = DateTimeOffset.UtcNow;
        var exp = now.AddSeconds(30).ToUnixTimeSeconds();
        var iat = now.ToUnixTimeSeconds();

        var metascopeClaimName = $"https://{integration.ImsEndpoint}/s/{integration.Metascopes}";

        var payloadDict = new Dictionary<string, object>
        {
            ["iss"] = integration.Org,
            ["sub"] = integration.Id,
            ["aud"] = $"https://{integration.ImsEndpoint}/c/{technicalAccount.ClientId}",
            ["exp"] = exp,
            ["iat"] = iat,
            [metascopeClaimName] = true
        };

        var headerJson = JsonConvert.SerializeObject(header);
        var payloadJson = JsonConvert.SerializeObject(payloadDict);

        static string B64Url(byte[] bytes)
            => Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');

        var headerB64 = B64Url(Encoding.UTF8.GetBytes(headerJson));
        var payloadB64 = B64Url(Encoding.UTF8.GetBytes(payloadJson));

        var signingInput = $"{headerB64}.{payloadB64}";
        var data = Encoding.UTF8.GetBytes(signingInput);

        var signature = rsaWrapper.Rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var signatureB64 = B64Url(signature);

        return $"{signingInput}.{signatureB64}";
    }

    private static string DeserializeAccessToken(string responseContent)
    {
        var accessTokenDto = JsonConvert.DeserializeObject<AccessTokenDto>(responseContent);
        if (accessTokenDto == null || string.IsNullOrEmpty(accessTokenDto.AccessToken))
        {
            throw new Exception($"Failed to get access token: {responseContent}");
        }

        return accessTokenDto.AccessToken;
    }
}