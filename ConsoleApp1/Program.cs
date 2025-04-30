using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

var json = @"{
    ""ok"": true,
    ""integration"": {
        ""imsEndpoint"": ""ims-na1.adobelogin.com"",
        ""metascopes"": ""ent_aem_cloud_api"",
        ""technicalAccount"": {
            ""clientId"": ""cm-p150602-e1601780-integration-0"",
            ""clientSecret"": ""p8e-xZ2jPNK30uqoe7YcC3Vnw39pwS3qSq6v""
        },
        ""email"": ""6be0e37b-dcea-4bbd-bd37-e651ab8aefec@techacct.adobe.com"",
        ""id"": ""ACEE1F5567EA441A0A495C33@techacct.adobe.com"",
        ""org"": ""AA8D1E0F67AA9B580A495C4F@AdobeOrg"",
        ""privateKey"": ""-----BEGIN RSA PRIVATE KEY-----\r\nMIIEpAIBAAKCAQEA3YdRbPtwnKdsvSCkJQzmBigktfg3+Q4Tx/i+NKGE++rKult9\r\n87qQmO2Xhzd2jZ+LuwfySXjENf7prKQaOLMsCvKYFXP4RFkQH0+03bps9imhZfOk\r\neRHp4bRdqjK6EOfq8iAwa52Uox+maP06KvX3dyBCG3METksZlt/cfipjqBxle64I\r\nlzvk3cyiOYjI3kWEM/wFHIaiaC2fJualcX8GFvmr9FPILQsDwKpID9aHhbciOkLl\r\n42fA5JhZcg/2Hnzq2sAAl7LVAWurG8Rj0YpLcuDAyDBxanFHoprpKDt+wGY2p90A\r\nf5HIUosLe5TeyuNyG7u9Rz53+89W7GUudEiUEwIDAQABAoIBAQCp5jlt4x3IF6QP\r\nHTSm8dCNEkathTSwf7puGPkP9ny+lKFs8fSUdBaoIziezMvQ7HdHN138OXIKk3n2\r\nHVm6+LejodFAStQy3ze9O+1UTMF6vgz52zXeYP3GTAW2HfeUNbp8fnGEZ7Pys7h2\r\nNxhgkIp9w7DPypOX1INIsmIyJSKPb+IBt7IrEiIUGWOtFj/XLUQIlVd8ZPLizVpI\r\nvVgISbpeSW3CVWJQkAlVvVh1MVwGuryg/Z6J1Mo0TXZjsPEdGxg8sRwlfgOc3NIy\r\nOKtAuFza105l6lKscn74yYCBYrgXIh18UV1pqPCLif5pSwqfVnjp8dmBRu2nY8Jq\r\ntwRds7XxAoGBAPxoHwxe2oQubeKlKe/Qe2hJbRqBoWDpPAFb+6wM6lOJrw1RkQr2\r\nzPvqyik7B5c0ROrEMzU5U8kFVNHwHAczmkO0nlLwsMFADWEJsd3uGLdm6DC30Knx\r\nKHNn1s4n4cPAvc8Jy2VA6gyGlYDYVgJrTzLZfle0b4AFcTLO4zeuOtDpAoGBAOCu\r\nqd90eT+wprYiY9MFdigW/AM7aTGq3c72RzvXPYtRoCUFdhaNWYTpGU+jA1I/+5Vy\r\nk4ZvLGE8IUYisBKE0P42nyFHy3o4nldbysr8bYVsUYEWu1y/KZcZcu2sIMfmiC/i\r\npkyIvozUbSkysYjt6yVrNKorDW/qgOiHAuSfdP+bAoGAJrm24cf/0L3q4BYlHAUp\r\nmfOCCMoQv3SpXzAEqf4FSbHbKLj1/u+kvZXlVHQZEwrS9A4MKUNVZocp31fuhPBW\r\n38JrdCA3jj7MjrHVF067fhAM2cSqABje8u3gmBqoWcdNl+FR0oQmJKvVbkJC1/Ys\r\n3YlfCAfH/6VWG8yAMf/KSFkCgYAE/Pu6gUx71IEvA/5xXeqsoy3/KF+Cgceg46jb\r\nNEEiibJjgAaKI6M8Jyyvru/Q8Ki2Pa/2yRsUIZCfjP+ZuacmLbJEu+JRmVYynFAZ\r\nR5dq4efBVO7d9USIHUGG805bAcw+O+rzQgnw+Hpf8scsQhP5ZbLqoEARHwSzpOO0\r\njbG8RwKBgQC0TMJPkPry+aKfCAzwHmB0sJFIametN7RYiPm8Vsm+WIW0Y0yERHFz\r\n64kwAq0PBZpIocgJNksw7oyXC4KWUrtGoSiPNglgz+6uU2qExv86Ufri64vfWiSc\r\nnwOuZrCvy3Vy0tf1Z2iuQqlYLHkemOpliS0dlneJ7V4kza8vcWOj4w==\r\n-----END RSA PRIVATE KEY-----\r\n"",
        ""publicKey"": ""-----BEGIN CERTIFICATE-----\r\nMIIDIDCCAgigAwIBAgIJZUsTUAh0HORzMA0GCSqGSIb3DQEBCwUAMCwxKjAoBgNV\r\nBAMTIWNtLXAxNTA2MDItZTE2MDE3ODAtaW50ZWdyYXRpb24tMDAeFw0yNTAzMzEw\r\nNzI4MjVaFw0yNjAzMzEwNzI4MjVaMCwxKjAoBgNVBAMTIWNtLXAxNTA2MDItZTE2\r\nMDE3ODAtaW50ZWdyYXRpb24tMDCCASIwDQYJKoZIhvcNAQEBBQADggEPADCCAQoC\r\nggEBAN2HUWz7cJynbL0gpCUM5gYoJLX4N/kOE8f4vjShhPvqyrpbffO6kJjtl4c3\r\ndo2fi7sH8kl4xDX+6aykGjizLArymBVz+ERZEB9PtN26bPYpoWXzpHkR6eG0Xaoy\r\nuhDn6vIgMGudlKMfpmj9Oir193cgQhtzBE5LGZbf3H4qY6gcZXuuCJc75N3MojmI\r\nyN5FhDP8BRyGomgtnybmpXF/Bhb5q/RTyC0LA8CqSA/Wh4W3IjpC5eNnwOSYWXIP\r\n9h586trAAJey1QFrqxvEY9GKS3LgwMgwcWpxR6Ka6Sg7fsBmNqfdAH+RyFKLC3uU\r\n3srjchu7vUc+d/vPVuxlLnRIlBMCAwEAAaNFMEMwDAYDVR0TBAUwAwEB/zALBgNV\r\nHQ8EBAMCAvQwJgYDVR0RBB8wHYYbaHR0cDovL2V4YW1wbGUub3JnL3dlYmlkI21l\r\nMA0GCSqGSIb3DQEBCwUAA4IBAQBCDWyOVlwm/U4Rmqrh5M3qtx4A3wLYvXSZ1caM\r\nE+bH0ZJUO1CDcuuBE7JyvVX3LHE07SE2E1HCNTm21PXKdmaQLwe1l5Qasd0UGfdW\r\nNHS8jmubeeX6i7XmYf+2YA7+8sbPmQ95+RTShnQXzTgbetjiiAez90GhJXxgY1jy\r\n5iGv7M76tKjflOwWe2fNhVf3UJb4vzXUD7CNwSLiHI1IWG9cll044t/qpiDGXTRo\r\n2sSWqtNskigBzdCwvrWJC3NEH0nZIgI7a5aJzV6OdENGf7MtrYM1onUxaiow5hI7\r\n9Aa9VQVPFgnUITPka+kjbMPSXFZmla1Wyd2EIElRyl8HunMt\r\n-----END CERTIFICATE-----\r\n"",
        ""certificateExpirationDate"": ""2026-03-31T07:28:25.000Z""
    },
    ""statusCode"": 200
}";
using var doc = JsonDocument.Parse(json);
var integration = doc.RootElement.GetProperty("integration");

// Extract private key from JSON
var privateKeyPem = integration.GetProperty("privateKey").GetString();

// Clean up potential \r\n encoding
privateKeyPem = privateKeyPem.Replace("\\r\\n", "\n").Replace("\\n", "\n");

using var rsa = RSA.Create();
rsa.ImportFromPem(privateKeyPem.ToCharArray());

// Generate token
var handler = new JwtSecurityTokenHandler();
var descriptor = new SecurityTokenDescriptor
{
    Issuer = integration.GetProperty("org").GetString(),
    Subject = new ClaimsIdentity(new[]
    {
        new Claim("sub", integration.GetProperty("id").GetString()),
        new Claim("https://ims-na1.adobelogin.com/s/ent_aem_cloud_api", "true", ClaimValueTypes.Boolean)
    }),
    Audience = $"https://ims-na1.adobelogin.com/c/{integration.GetProperty("technicalAccount").GetProperty("clientId").GetString()}",
    Expires = DateTime.UtcNow.AddMinutes(5),
    SigningCredentials = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256)
};

var token = handler.CreateToken(descriptor);
string jwt = handler.WriteToken(token);
Console.WriteLine(jwt);
File.WriteAllText(@"E:\dev\blackbird\AdobeExperienceManager\ConsoleApp1\token.txt", jwt);