using Volo.Abp.Modularity;

namespace BioTrace.Elsa.Abp;

/* Inherit from this class for your domain layer tests.
 * See SampleManager_Tests for example.
 */
public abstract class AbpDomainTestBase<TStartupModule> : AbpTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
