using Apps.AEM.Constants;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Utils.Extensions.Sdk;
using Blackbird.Applications.Sdk.Utils.RestSharp;
using Newtonsoft.Json;
using RestSharp;

namespace Apps.AEM.Api;

public class Client : BlackBirdRestClient
{
    public Client(IEnumerable<AuthenticationCredentialsProvider> creds) : base(new()
    {
        BaseUrl = new Uri(""),
    })
    {
        this.AddDefaultHeader("Authorization", creds.Get(CredNames.Token).Value);
    }

    protected override Exception ConfigureErrorException(RestResponse response)
    {
        var error = JsonConvert.DeserializeObject(response.Content!);
        var errorMessage = "";

        throw new PluginApplicationException(errorMessage);
    }
}
