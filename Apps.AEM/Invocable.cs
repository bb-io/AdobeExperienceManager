using Apps.AEM.Api;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.AEM;

public class Invocable : BaseInvocable
{
    protected AuthenticationCredentialsProvider[] Credentials =>
        InvocationContext.AuthenticationCredentialsProviders.ToArray();
    protected Client Client { get; }
    
    public Invocable(InvocationContext invocationContext) : base(invocationContext)
    {
        Client = new(Credentials);
    }
}
