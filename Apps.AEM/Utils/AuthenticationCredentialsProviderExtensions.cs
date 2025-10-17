using Apps.AEM.Constants;
using Apps.AEM.Models.Dtos;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Utils.Extensions.Sdk;
using Newtonsoft.Json;

namespace Apps.AEM.Utils;

public static class AuthenticationCredentialsProviderExtensions
{
    public static string GetConnectionType(this IEnumerable<AuthenticationCredentialsProvider> credentialsProviders)
    {
        return GetCredentialValue(credentialsProviders, CredNames.ConnectionType, "Connection type");
    }

    public static string GetBaseUrl(this IEnumerable<AuthenticationCredentialsProvider> credentialsProviders)
    {
        var baseUrl = GetCredentialValue(credentialsProviders, CredNames.BaseUrl, "Base URL");
        return baseUrl.TrimEnd('/');
    }

    public static string GetUsername(this IEnumerable<AuthenticationCredentialsProvider> credentialsProviders)
    {
        var username = GetCredentialValue(credentialsProviders, CredNames.Username, "Username");
        return username;
    }

    public static string GetPassword(this IEnumerable<AuthenticationCredentialsProvider> credentialsProviders)
    {
        var password = GetCredentialValue(credentialsProviders, CredNames.Password, "Password");
        return password;
    }

    public static string GetJsonCertificate(this IEnumerable<AuthenticationCredentialsProvider> credentialsProviders)
    {
        var jsonCertificate = GetCredentialValue(credentialsProviders, CredNames.IntegrationJsonCertificate, "Integration JSON certificate");
        return jsonCertificate;
    }

    public static string GetImsEndpoint(this IEnumerable<AuthenticationCredentialsProvider> credentialsProviders)
    {
        var jsonCertificate = GetCredentialValue(credentialsProviders, CredNames.IntegrationJsonCertificate, "Integration JSON certificate");
        var authCertificateDto = DeserializeJsonCertificate(jsonCertificate);
        
        if (authCertificateDto.Integration?.ImsEndpoint == null)
        {
            throw new ArgumentNullException($"Integration JSON certificate is not valid: {jsonCertificate}");
        }
        
        return authCertificateDto.Integration.ImsEndpoint;
    }

    public static TechnicalAccountDto GetTechnicalAccount(this IEnumerable<AuthenticationCredentialsProvider> credentialsProviders)
    {
        var jsonCertificate = GetCredentialValue(credentialsProviders, CredNames.IntegrationJsonCertificate, "Integration JSON certificate");
        var authCertificateDto = DeserializeJsonCertificate(jsonCertificate);
        
        if (authCertificateDto.Integration?.TechnicalAccount == null)
        {
            throw new ArgumentNullException($"Integration JSON certificate is not valid: {jsonCertificate}");
        }
        
        return authCertificateDto.Integration.TechnicalAccount;
    }

    private static string GetCredentialValue(IEnumerable<AuthenticationCredentialsProvider> credentialsProviders, string credName, string credDisplayName)
    {
        var credValue = credentialsProviders.Get(credName);
        if (credValue == null || string.IsNullOrEmpty(credValue.Value))
        {
            throw new ArgumentNullException($"{credDisplayName} is not provided in the credentialsProviders.");
        }
        
        return credValue.Value;
    }

    private static AuthCertificateDto DeserializeJsonCertificate(string jsonCertificate)
    {
        var authCertificateDto = JsonConvert.DeserializeObject<AuthCertificateDto>(jsonCertificate);
        if (authCertificateDto == null)
        {
            throw new ArgumentNullException($"Integration JSON certificate is not valid: {jsonCertificate}");
        }
        
        return authCertificateDto;
    }
}
