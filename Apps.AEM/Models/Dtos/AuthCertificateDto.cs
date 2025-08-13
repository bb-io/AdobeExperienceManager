using Newtonsoft.Json;

namespace Apps.AEM.Models.Dtos;
public class AuthCertificateDto
{
    [JsonProperty("ok")]
    public bool Ok { get; set; }

    [JsonProperty("integration")]
    public IntegrationDto Integration { get; set; } = new();

    [JsonProperty("statusCode")]
    public int StatusCode { get; set; }
}

public class IntegrationDto
{
    [JsonProperty("imsEndpoint")]
    public string ImsEndpoint { get; set; } = string.Empty;

    [JsonProperty("metascopes")]
    public string Metascopes { get; set; } = string.Empty;

    [JsonProperty("technicalAccount")]
    public TechnicalAccountDto TechnicalAccount { get; set; } = new();

    [JsonProperty("email")]
    public string Email { get; set; } = string.Empty;

    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("org")]
    public string Org { get; set; } = string.Empty;

    [JsonProperty("privateKey")]
    public string PrivateKey { get; set; } = string.Empty;

    [JsonProperty("publicKey")]
    public string PublicKey { get; set; } = string.Empty;

    [JsonProperty("certificateExpirationDate")]
    public DateTime CertificateExpirationDate { get; set; }
}

public class TechnicalAccountDto
{
    [JsonProperty("clientId")]
    public string ClientId { get; set; } = string.Empty;

    [JsonProperty("clientSecret")]
    public string ClientSecret { get; set; } = string.Empty;
}