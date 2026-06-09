namespace BioTrace.Elsa.Abp.MultiTenancy;

public interface IElsaAbpTenantMapper
{
    string ToElsaTenantId(Guid? abpTenantId);

    Guid? ToAbpTenantId(string? elsaTenantId);
}
