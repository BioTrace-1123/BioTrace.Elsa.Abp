using Microsoft.Extensions.Configuration;
using NSubstitute;
using OpenIddict.Abstractions;
using Shouldly;
using Volo.Abp.Uow;
using Xunit;

namespace BioTrace.Elsa.Abp.Data;

public class ElsaAbpOpenIddictDataSeedContributor_Tests
{
    [Fact]
    public async Task SeedAsync_should_create_swagger_client_from_configuration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OpenIddict:Applications:MyApp_Swagger:ClientId"] = "MyApp_Swagger",
                ["OpenIddict:Applications:MyApp_Swagger:RootUrl"] = "https://localhost:44388"
            })
            .Build();

        var applicationManager = Substitute.For<IOpenIddictApplicationManager>();
        applicationManager.FindByClientIdAsync("MyApp_Swagger")
            .Returns(ValueTask.FromResult<object?>(null));

        var scopeManager = Substitute.For<IOpenIddictScopeManager>();
        scopeManager.FindByNameAsync("BioTrace_Elsa_Abp")
            .Returns(ValueTask.FromResult<object?>(null));

        var contributor = new ElsaAbpOpenIddictDataSeedContributor(
            configuration,
            applicationManager,
            scopeManager,
            CreateUnitOfWorkManager());

        await contributor.SeedAsync(new Volo.Abp.Data.DataSeedContext());

        await applicationManager.Received(1).CreateAsync(
            Arg.Is<OpenIddictApplicationDescriptor>(descriptor =>
                descriptor.ClientId == "MyApp_Swagger"
                && descriptor.RedirectUris.Any(uri =>
                    uri.AbsoluteUri == "https://localhost:44388/swagger/oauth2-redirect.html")));
    }

    [Fact]
    public async Task SeedAsync_should_update_redirect_uris_when_application_exists()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OpenIddict:Applications:ElsaStudio:ClientId"] = "ElsaStudio",
                ["OpenIddict:Applications:ElsaStudio:RedirectUris:0"] =
                    "https://localhost:44388/studio/authentication/login-callback"
            })
            .Build();

        var existing = new object();
        var applicationManager = Substitute.For<IOpenIddictApplicationManager>();
        applicationManager.FindByClientIdAsync("ElsaStudio")
            .Returns(ValueTask.FromResult<object?>(existing));
        applicationManager.PopulateAsync(Arg.Any<OpenIddictApplicationDescriptor>(), existing)
            .Returns(callInfo =>
            {
                var descriptor = callInfo.ArgAt<OpenIddictApplicationDescriptor>(0);
                descriptor.RedirectUris.Add(new Uri("https://old.example/studio/authentication/login-callback"));
                return ValueTask.CompletedTask;
            });

        var scopeManager = Substitute.For<IOpenIddictScopeManager>();
        scopeManager.FindByNameAsync("BioTrace_Elsa_Abp")
            .Returns(ValueTask.FromResult<object?>(new object()));

        var contributor = new ElsaAbpOpenIddictDataSeedContributor(
            configuration,
            applicationManager,
            scopeManager,
            CreateUnitOfWorkManager());

        await contributor.SeedAsync(new Volo.Abp.Data.DataSeedContext());

        await applicationManager.Received(1).UpdateAsync(existing, Arg.Any<OpenIddictApplicationDescriptor>());
    }

    private static IUnitOfWorkManager CreateUnitOfWorkManager()
    {
        var uow = Substitute.For<IUnitOfWork>();
        uow.CompleteAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var uowManager = Substitute.For<IUnitOfWorkManager>();
        uowManager.Begin(requiresNew: true, isTransactional: true).Returns(uow);

        return uowManager;
    }
}
