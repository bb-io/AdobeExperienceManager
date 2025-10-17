using System.Text;
using Apps.AEM.Services;
using Tests.AEM.Base;
using Apps.AEM.Constants;
using Apps.AEM.Utils;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Tests.AEM;

[TestClass]
public class CloudTokenServiceTests : TestBase
{
    // Added: Property to select the first Cloud invocation context
    private InvocationContext CloudContext => InvocationContexts
        .FirstOrDefault(ctx => ctx.AuthenticationCredentialsProviders.GetConnectionType() == ConnectionTypes.Cloud)
        ?? throw new InvalidOperationException("No cloud connection type found in configuration.");

    [TestMethod]
    public async Task GetAccessToken_ValidCredentials_ShouldReturnToken()
    {
        // Act
        var token = await TokenService.GetAccessTokenAsync(CloudContext.AuthenticationCredentialsProviders);
        
        // Assert
        Assert.IsFalse(string.IsNullOrEmpty(token));

        var memory = new MemoryStream(Encoding.UTF8.GetBytes(token));
        await FileManager.UploadAsync(memory, "plain/text", "bearer_token.txt");

        TestContext.WriteLine($"Successfully retrieved token: {token[..10]}…");
    }

    [TestMethod]
    public async Task GetJwtToken_ValidCredentials_ShouldReturnJwtToken()
    {
        // Act
        var token = TokenService.GetJwtToken(CloudContext.AuthenticationCredentialsProviders);
        
        // Assert
        Assert.IsFalse(string.IsNullOrEmpty(token));

        var memory = new MemoryStream(Encoding.UTF8.GetBytes(token));
        await FileManager.UploadAsync(memory, "plain/text", "jwt_token.txt");

        TestContext.WriteLine($"Successfully retrieved token: {token[..10]}…");
    }
}
