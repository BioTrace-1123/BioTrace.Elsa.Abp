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

## 5. 仓库演示项目集成测（T0–T11）

项目 [`test/BioTrace.Elsa.Abp.HttpApi.Host.Tests`](../../test/BioTrace.Elsa.Abp.HttpApi.Host.Tests/) 通过 `WebApplicationFactory` + **Password Grant 测试客户端**（仅测试配置启用）验证 Contributor 桥接、OpenIddict 验签与 Elsa API 鉴权。

**测试客户端**（仅 WAF 注入，不在演示 `appsettings.json` 中）：

| 项 | 值 |
|----|-----|
| ClientId | `BioTrace_Elsa_Abp_IntegrationTests` |
| ClientSecret | `integration-test-secret` |
| 开关 | `AuthServer:AllowPasswordGrantForIntegrationTests=true` |

**用例矩阵**

对应 [`ElsaAbpPermissionBridgeIntegrationTests`](../../test/BioTrace.Elsa.Abp.HttpApi.Host.Tests/Security/ElsaAbpPermissionBridgeIntegrationTests.cs) 与 [`ElsaAbpMultiTenancyIntegrationTests`](../../test/BioTrace.Elsa.Abp.HttpApi.Host.Tests/MultiTenancy/ElsaAbpMultiTenancyIntegrationTests.cs)：

| # | 场景 | 断言 |
|---|------|------|
| T0 | 无 Bearer Token | `GET /elsa/api/workflow-definitions` → 401 |
| T1 | `tenant-a-designer` Password Grant Token | JWT `permissions` **无** `*` / `write:workflow-definitions` |
| T2 | admin Password Grant Token + `__tenant: tenant-a` | `GET /identity/users/me` → 200，`permissions` 含 `*` |
| T3 | admin Password Grant Token | `GET /elsa/api/workflow-definitions` → 200 |
| T4 | `tenant-a-designer` Token + `__tenant` | `GET /identity/users/me` → 200，仅 read claims |
| T5 | `tenant-a-designer` Token + `__tenant` | `POST /elsa/api/workflow-definitions` → 403 |
| T6 | `tenant-a-admin` Token + `__tenant` | `GET /elsa/api/descriptors/commit-strategies/workflows` → 200 |
| T7 | 租户 A admin 创建定义 + 租户 B 列表 | 租户 B **不应**看到租户 A 的 `definitionId` |
| T8 | Host admin 列表（无 `__tenant`） | **不应**看到租户内工作流定义 |
| T9 | `tenant-a-admin` Password Grant Token | JWT 含 `tenantid` Claim |
| T10 | Host admin + `__tenant: tenant-a` 列表 | **应**看到在 tenant-a 下创建的定义 |
| T11 | Host admin + `__tenant: tenant-b` 列表 | **不应**看到仅在 tenant-a 下的定义 |

**运行**

```bash
# 宿主机（先 docker compose up -d）
./scripts/test-integration.sh

# Dev Container 内（Postgres 主机为 postgres，环境变量已配置）
./scripts/test-integration.sh
```

连接串主机可通过环境变量 `INTEGRATION_TEST_POSTGRES_HOST` 覆盖（默认 `localhost`）。测试库 `BioTrace_Abp_Test`、`BioTrace_Elsa_Test` 在 Postgres 可达时由测试 fixture 自动创建（亦见 [`docker/postgres/init/01-create-databases.sql`](../../docker/postgres/init/01-create-databases.sql)）。
