using Apps.AEM.Utils;
using Blackbird.Applications.Sdk.Common.Authentication;
using RestSharp;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using Apps.AEM.Models.Dtos;
using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace Apps.AEM.Services;

public static class TokenService
{
    private static readonly ConcurrentDictionary<string, TokenCacheItem> AccessTokenCache = new();
    private static readonly ConcurrentDictionary<string, RSAWrapper> RsaCache = new();
    private static readonly TimeSpan DefaultTokenLifetime = TimeSpan.FromMinutes(55);

    private class TokenCacheItem
    {
        public string Token { get; set; }
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
        
        // Try to get from cache first
        if (AccessTokenCache.TryGetValue(cacheKey, out var cacheItem) && !cacheItem.IsExpired)
        {
            return cacheItem.Token;
        }
        
        // If not in cache or expired, get a new token
        var tokenRequest = new RestRequest("/ims/exchange/jwt", Method.Post);

        tokenRequest.AddHeader("Cache-Control", "no-cache");
        tokenRequest.AlwaysMultipartFormData = true;
        
        tokenRequest.AddParameter("client_id", technicalAccount.ClientId);
        tokenRequest.AddParameter("client_secret", technicalAccount.ClientSecret);
        tokenRequest.AddParameter("jwt_token", GetJwtToken(credentials));
        
        var imsEndpoint = credentials.GetImsEndpoint();
        var client = new RestClient($"https://{imsEndpoint}");
        var response = await client.ExecuteAsync(tokenRequest);
        if (!response.IsSuccessful)
        {
            throw new Exception($"Failed to get access token: {response.ErrorMessage} - {response.Content}");
        }

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
        
        var authCertificateDto = JsonConvert.DeserializeObject<AuthCertificateDto>(jsonCertificate);
        if (authCertificateDto == null || authCertificateDto.Integration == null)
        {
            throw new ArgumentNullException($"Integration JSON certificate is not valid: {jsonCertificate}");
        }
        
        var integration = authCertificateDto.Integration;
        var privateKeyPem = integration.PrivateKey;
        privateKeyPem = privateKeyPem.Replace("\\r\\n", "\n").Replace("\\n", "\n");
        
        // Cache key for RSA object
        string rsaCacheKey = technicalAccount.ClientId;
        
        // Get or create RSA wrapper
        var rsaWrapper = RsaCache.GetOrAdd(rsaCacheKey, _ => new RSAWrapper(privateKeyPem));
        
        var handler = new JwtSecurityTokenHandler();
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = integration.Org,
            Subject = new ClaimsIdentity(
            [
                new Claim("sub", integration.Id),
                new Claim($"https://{integration.ImsEndpoint}/s/{integration.Metascopes}", "true", ClaimValueTypes.Boolean)
            ]),
            Audience = $"https://{integration.ImsEndpoint}/c/{technicalAccount.ClientId}",
            Expires = DateTime.UtcNow.AddSeconds(30),
            SigningCredentials = new SigningCredentials(new RsaSecurityKey(rsaWrapper.Rsa), SecurityAlgorithms.RsaSha256)
        };
        
        var token = handler.CreateToken(descriptor);
        return handler.WriteToken(token);
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
    
    // Method to clear cache if needed
    public static void ClearTokenCache()
    {
        AccessTokenCache.Clear();
        
        // Dispose RSA instances before clearing the cache
        foreach (var wrapper in RsaCache.Values)
        {
            try
            {
                wrapper.Dispose();
            }
            catch
            {
                // Ignore disposal errors
            }
        }
        RsaCache.Clear();
    }
}