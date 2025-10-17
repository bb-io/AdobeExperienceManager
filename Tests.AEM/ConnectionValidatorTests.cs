using Apps.AEM.Connections;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Invocation;
using System.Reflection;
using Tests.AEM.Base;

namespace Tests.AEM;

[TestClass]
public class ConnectionValidatorTests : TestBase
{
    public TestContext TestContext { get; set; }

    // can't use parent method directly in DynamicData decorator as studio can't see it and shows a warning
    public static string? GetConnectionTypeName(MethodInfo _, object[]? data) => GetConnectionTypeFromDynamicData(data);

    [TestMethod]
    [DynamicData(nameof(AllInvocationContexts), DynamicDataDisplayName = nameof(GetConnectionTypeName))]
    public async Task ValidateConnection_ValidData_ShouldBeSuccessful(InvocationContext context)
    {
        var validator = new ConnectionValidator();
        var result = await validator.ValidateConnection(
            context.AuthenticationCredentialsProviders,
            CancellationToken.None);

        TestContext.WriteLine(result.Message);
        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    [DynamicData(nameof(AllInvocationContexts), DynamicDataDisplayName = nameof(GetConnectionTypeName))]
    public async Task ValidateConnection_InvalidData_ShouldFail(InvocationContext context)
    {
        var validator = new ConnectionValidator();

        var newCredentials = context.AuthenticationCredentialsProviders
            .Select(x => new AuthenticationCredentialsProvider(x.KeyName, x.Value + "_incorrect"));

        var result = await validator.ValidateConnection(newCredentials, CancellationToken.None);

        TestContext.WriteLine(result.Message);
        Assert.IsFalse(result.IsValid);
    }
}
