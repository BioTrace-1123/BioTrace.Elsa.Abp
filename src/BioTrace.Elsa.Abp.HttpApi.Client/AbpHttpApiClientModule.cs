using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Http.Client;
using Volo.Abp.Modularity;
using Volo.Abp.VirtualFileSystem;

namespace BioTrace.Elsa.Abp;

[DependsOn(
    typeof(AbpApplicationContractsModule),
    typeof(AbpHttpClientModule))]
public class AbpHttpApiClientModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddHttpClientProxies(
            typeof(AbpApplicationContractsModule).Assembly,
            AbpRemoteServiceConsts.RemoteServiceName
        );

        Configure<AbpVirtualFileSystemOptions>(options =>
        {
            options.FileSets.AddEmbedded<AbpHttpApiClientModule>();
        });

    }
}
