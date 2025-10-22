using Apps.AEM.Utils;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Invocation;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace Tests.AEM.Base;
public class TestBase
{
    public List<IEnumerable<AuthenticationCredentialsProvider>> CredentialGroups { get; init; }

    public List<InvocationContext> InvocationContexts { get; init; }

    public FileManager FileManager { get; init; }

    public TestContext? TestContext { get; set; }

    protected TestBase()
    {
        var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

        CredentialGroups = config
            .GetSection("ConnectionDefinition").GetChildren()
            .Select(section =>
                section.GetChildren()
               .Select(child => new AuthenticationCredentialsProvider(child.Key, child.Value!))
            ).ToList();

        InvocationContexts = [];
        foreach (var credentialGroup in CredentialGroups)
        {
            InvocationContexts.Add(new InvocationContext
            {
                AuthenticationCredentialsProviders = credentialGroup
            });
        }

        FileManager = new FileManager();
    }

    public static IEnumerable<object[]> AllInvocationContexts
    {
        get
        {
            var testBase = new TestBase();
            return testBase.InvocationContexts.Select(ctx => new object[] { ctx });
        }
    }

    public static string? GetConnectionTypeFromDynamicData(object[]? data)
    {
        var context = data?[0] as InvocationContext
             ?? throw new ArgumentNullException(nameof(data));
        return "Connection type: " + context.AuthenticationCredentialsProviders.GetConnectionType();
    }

    private static JsonSerializerOptions PrintResultOptions => new() { WriteIndented = true };

    public void PrintResult(object? obj)
    {
        TestContext?.WriteLine(JsonSerializer.Serialize(obj, PrintResultOptions));
    }
}
