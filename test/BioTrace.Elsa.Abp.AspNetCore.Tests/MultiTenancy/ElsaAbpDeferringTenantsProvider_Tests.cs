using Elsa.Common.Multitenancy;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Volo.Abp.MultiTenancy;
using Volo.Abp.TenantManagement;
using Xunit;
using AbpTenant = Volo.Abp.TenantManagement.Tenant;

namespace BioTrace.Elsa.Abp.MultiTenancy;

public class ElsaAbpDeferringTenantsProvider_Tests
{
    [Fact]
    public async Task ListAsync_should_return_host_only_when_inner_throws_and_defer_enabled()
    {
        var repository = Substitute.For<ITenantRepository>();
        repository.GetListAsync(cancellationToken: Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("relation does not exist"));

        var provider = CreateDeferringProvider(repository, defer: true);

        var tenants = (await provider.ListAsync()).ToList();

        tenants.Count.ShouldBe(1);
        tenants[0].Name.ShouldBe("Host");
    }

    [Fact]
    public async Task ListAsync_should_query_repository_when_defer_disabled()
    {
        var repository = Substitute.For<ITenantRepository>();
        repository.GetListAsync(cancellationToken: Arg.Any<CancellationToken>())
            .Returns(new List<AbpTenant>());

        var provider = CreateDeferringProvider(repository, defer: false);
        var tenants = (await provider.ListAsync()).ToList();

        tenants.Count.ShouldBe(1);
        tenants[0].Name.ShouldBe("Host");
        await repository.Received(1).GetListAsync(cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FindAsync_should_return_null_when_inner_throws_for_tenant_lookup()
    {
        var repository = Substitute.For<ITenantRepository>();
        repository.FindAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("relation does not exist"));

        var provider = CreateDeferringProvider(repository, defer: true);
        var tenantId = Guid.NewGuid().ToString("D");

        var tenant = await provider.FindAsync(TenantFilter.ById(tenantId));

        tenant.ShouldBeNull();
    }

    private static ElsaAbpDeferringTenantsProvider CreateDeferringProvider(
        ITenantRepository repository,
        bool defer)
    {
        var currentTenant = Substitute.For<ICurrentTenant>();
        var mapper = new ElsaAbpTenantMapper(Options.Create(new ElsaAbpOptions()));
        var inner = new ElsaAbpTenantsProvider(repository, mapper, currentTenant);

        return new ElsaAbpDeferringTenantsProvider(
            inner,
            mapper,
            Options.Create(new ElsaAbpOptions { DeferTenantActivationUntilReady = defer }));
    }
}
