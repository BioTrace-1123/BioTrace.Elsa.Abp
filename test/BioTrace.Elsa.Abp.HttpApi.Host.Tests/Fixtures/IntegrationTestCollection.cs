using Xunit;

namespace BioTrace.Elsa.Abp.Fixtures;

[CollectionDefinition(Name)]
public class IntegrationTestCollection : ICollectionFixture<ElsaAbpWebApplicationFactory>
{
    public const string Name = "HostIntegration";
}
