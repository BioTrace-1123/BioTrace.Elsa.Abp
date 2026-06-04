using Microsoft.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;

namespace BioTrace.Elsa.Abp.EntityFrameworkCore;

[ConnectionStringName(AbpDbProperties.ConnectionStringName)]
public class AbpDbContext : AbpDbContext<AbpDbContext>, IAbpDbContext
{
    public AbpDbContext(DbContextOptions<AbpDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ConfigureAbp();
    }
}
