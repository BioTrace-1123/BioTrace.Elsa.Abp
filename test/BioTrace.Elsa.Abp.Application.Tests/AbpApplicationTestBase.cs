using Volo.Abp.Modularity;

namespace BioTrace.Elsa.Abp;

/* Inherit from this class for your application layer tests.
 * See SampleAppService_Tests for example.
 */
public abstract class AbpApplicationTestBase<TStartupModule> : AbpTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
