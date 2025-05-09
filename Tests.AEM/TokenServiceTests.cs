using System.Text;
using Apps.AEM.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tests.AEM.Base;

namespace Tests.AEM;

[TestClass]
public class TokenServiceTests : TestBase
{
    [TestMethod]
    public async Task GetAccessToken_ValidCredentials_ShouldReturnToken()
    {
        // Act
        var token = await TokenService.GetAccessTokenAsync(Creds);
        
        // Assert
        Assert.IsFalse(string.IsNullOrEmpty(token));

        var memory = new MemoryStream(Encoding.UTF8.GetBytes(token));
        await FileManager.UploadAsync(memory, "plain/text", "bearer_token.txt");

        Console.WriteLine($"Successfully retrieved token: {token.Substring(0, 15)}...");
    }

    [TestMethod]
    public async Task GetJwtToken_ValidCredentials_ShouldReturnJwtToken()
    {
        // Act
        var token = TokenService.GetJwtToken(Creds);
        
        // Assert
        Assert.IsFalse(string.IsNullOrEmpty(token));

        var memory = new MemoryStream(Encoding.UTF8.GetBytes(token));
        await FileManager.UploadAsync(memory, "plain/text", "jwt_token.txt");

        Console.WriteLine($"Successfully retrieved token: {token.Substring(0, 15)}...");
    }
}
