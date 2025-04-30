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
        var token = await TokenService.GetAccessToken(Creds);
        
        // Assert
        Assert.IsFalse(string.IsNullOrEmpty(token));

        var memory = new MemoryStream(Encoding.UTF8.GetBytes(token));
        await FileManager.UploadAsync(memory, "plain/text", "token.txt");

        Console.WriteLine($"Successfully retrieved token: {token.Substring(0, 15)}...");
    }
}
