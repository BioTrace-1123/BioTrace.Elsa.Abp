# 调用方 HttpApi.Host 集成测试样板

本目录说明如何在外部 ABP 解决方案中引用 `BioTrace.Elsa.Abp.IntegrationTesting` 建立最小集成测。

## 1. 创建测试项目

```bash
dotnet new xunit -n MyCompany.MyApp.HttpApi.Host.Tests -o test/MyCompany.MyApp.HttpApi.Host.Tests
cd test/MyCompany.MyApp.HttpApi.Host.Tests
dotnet add reference ../../src/MyCompany.MyApp.HttpApi.Host/MyCompany.MyApp.HttpApi.Host.csproj
dotnet add package BioTrace.Elsa.Abp.IntegrationTesting
dotnet add package Microsoft.AspNetCore.Mvc.Testing
dotnet add package Shouldly
dotnet add package xunit
```

## 2. WebApplicationFactory

```csharp
using BioTrace.Elsa.Abp.IntegrationTesting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MyCompany.MyApp;

public class MyAppWebApplicationFactory : ElsaAbpIntegrationTestWebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Host=localhost;...",
                ["ConnectionStrings:Elsa"] = "Host=localhost;...",
                ["Elsa:RunMigrations"] = "true",
                ["OpenIddict:Applications:IntegrationTests:ClientId"] = "IntegrationTests",
                ["OpenIddict:Applications:IntegrationTests:ClientSecret"] = "test-secret"
            });
        });
    }
}
```

## 3. 示例测试

```csharp
public class ElsaApi_Tests : IClassFixture<MyAppWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ElsaApi_Tests(MyAppWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_should_return_ok()
    {
        var response = await _client.GetAsync("/health-status");
        response.IsSuccessStatusCode.ShouldBeTrue();
    }
}
```

## 4. OpenIddict 种子

集成测 Password Grant 客户端须在 Host/DbMigrator 种子中配置 `IntegrationTests` 应用（可继承 `ElsaAbpOpenIddictDataSeedContributor`）。

完整演示见仓库 [`test/BioTrace.Elsa.Abp.HttpApi.Host.Tests`](../../test/BioTrace.Elsa.Abp.HttpApi.Host.Tests/)。
