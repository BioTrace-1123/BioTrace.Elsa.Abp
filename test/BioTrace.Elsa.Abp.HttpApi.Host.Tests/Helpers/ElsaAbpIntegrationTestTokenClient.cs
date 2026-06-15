using BioTrace.Elsa.Abp.IntegrationTesting;

namespace BioTrace.Elsa.Abp.Helpers;

public class ElsaAbpIntegrationTestTokenClient : OpenIddictTokenClient
{
    public const string ClientId = "BioTrace_Elsa_Abp_IntegrationTests";
    public const string ClientSecret = "integration-test-secret";

    public ElsaAbpIntegrationTestTokenClient(HttpClient httpClient)
        : base(httpClient, ClientId, ClientSecret, "BioTrace_Elsa_Abp openid profile roles")
    {
    }
}
