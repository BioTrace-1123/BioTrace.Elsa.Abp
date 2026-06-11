using BioTrace.Elsa.Abp.MultiTenancy;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace BioTrace.Elsa.Abp.MultiTenancy;

public class ElsaAbpTenantMapper_Tests
{
    private readonly ElsaAbpTenantMapper _mapper = new(Options.Create(new ElsaAbpOptions()));

    [Fact]
    public void ToElsaTenantId_should_map_host_to_empty_string()
    {
        _mapper.ToElsaTenantId(null).ShouldBe(string.Empty);
    }

    [Fact]
    public void ToElsaTenantId_should_map_guid_to_standard_format()
    {
        var tenantId = Guid.Parse("11111111-1111-1111-1111-111111111101");

        _mapper.ToElsaTenantId(tenantId).ShouldBe("11111111-1111-1111-1111-111111111101");
    }

    [Fact]
    public void ToAbpTenantId_should_roundtrip_guid_values()
    {
        var tenantId = Guid.Parse("11111111-1111-1111-1111-111111111101");
        var elsaTenantId = _mapper.ToElsaTenantId(tenantId);

        _mapper.ToAbpTenantId(elsaTenantId).ShouldBe(tenantId);
    }

    [Fact]
    public void ToAbpTenantId_should_map_empty_string_to_null()
    {
        _mapper.ToAbpTenantId(string.Empty).ShouldBeNull();
    }
}
