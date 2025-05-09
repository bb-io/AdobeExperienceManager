using Apps.AEM.Utils;
using Blackbird.Applications.Sdk.Common.Authentication;
using RestSharp;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using Apps.AEM.Models.Dtos;
using Newtonsoft.Json;

namespace Apps.AEM.Services;

public static class TokenService
{
    public static async Task<string> GetAccessTokenAsync(IEnumerable<AuthenticationCredentialsProvider> credentials)
    {
        var tokenRequest = new RestRequest("/ims/exchange/jwt", Method.Post);
        var technicalAccount = credentials.GetTechnicalAccount();

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

        return DeserializeAccessToken(response.Content!);
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
        
        using var rsa = RSA.Create();
        rsa.ImportFromPem(privateKeyPem.ToCharArray());
        
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
            SigningCredentials = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256)
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
}