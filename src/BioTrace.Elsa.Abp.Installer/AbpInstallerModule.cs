using Volo.Abp.Modularity;
using Volo.Abp.VirtualFileSystem;

namespace BioTrace.Elsa.Abp;

[DependsOn(
    typeof(AbpVirtualFileSystemModule)
    )]
public class AbpInstallerModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpVirtualFileSystemOptions>(options =>
        {
            options.FileSets.AddEmbedded<AbpInstallerModule>();
        });
    }
}
