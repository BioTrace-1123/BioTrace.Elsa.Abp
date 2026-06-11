# 调用方集成指南（NuGet）

本文说明如何在**既有 ABP 调用方项目**中引用本模块 NuGet 包，完成 **Elsa Workflows** 与 **Elsa Studio** 集成（权限桥接、双库、OpenIddict）。

本模块**不提供**自有业务实体或 ABP 业务 DbContext；Identity / OpenIddict / Permission 等由调用方既有模块与数据库维护。仓库内 `host/BioTrace.Elsa.Abp.HttpApi.Host` **仅作本地验证与测试**，不发布 NuGet，可参考其实现但不需复制其全部 DependsOn。

> 演示项目：[`AbpHttpApiHostModule`](../host/BioTrace.Elsa.Abp.HttpApi.Host/AbpHttpApiHostModule.cs)、[`ElsaAbpHostPostgreSqlModule`](../host/BioTrace.Elsa.Abp.HttpApi.Host/ElsaAbpHostPostgreSqlModule.cs)

## 前置条件

| 项 | 要求 |
|---|---|
| .NET SDK | 10（见仓库根目录 `global.json`） |
| ABP | 10.4.x |
| 数据库 | ABP 业务库：PostgreSQL / SQL Server 等（与调用方 ABP 配置一致）；Elsa 工作流库：由所选 `Elsa.Persistence.EFCore.{Provider}` 决定（演示项目使用 PostgreSQL 15+） |
| 认证 | OpenIddict（本模块不启用 `Elsa.Identity`，由调用方 JWT 统一保护 ABP 与 Elsa API） |
| 权限 | ABP Permission Management（角色/用户授权 + 请求时映射到 Elsa `permissions` Claim） |

## 安装 NuGet 包

### Elsa Workflows 集成（最小）

在调用方 Web 项目（如 `MyApp.HttpApi.Host`）中安装：

```xml
<ItemGroup>
  <PackageReference Include="BioTrace.Elsa.Abp.AspNetCore" Version="1.0.0" />
  <PackageReference Include="BioTrace.Elsa.Abp.HttpApi" Version="1.0.0" />
  <!-- Elsa EF Provider（与 Elsa 3.7.0 成套，三选一） -->
  <PackageReference Include="Elsa.Persistence.EFCore.PostgreSql" Version="3.7.0" />
</ItemGroup>
```

- `BioTrace.Elsa.Abp.AspNetCore` 传递依赖 `Application`、`Domain`、`Application.Contracts`、`Domain.Shared`，一般无需逐个引用。
- **不要**安装 `BioTrace.Elsa.Abp.EntityFrameworkCore`（仓库内仅供演示项目 / 单元测，**不发布** NuGet）；ABP 业务库迁移使用调用方**自有** DbContext（Identity、OpenIddict、Permission 等）。
- **不会**传递 `Elsa.Persistence.EFCore.{Provider}`，调用方必须自选 Provider 包。

### Elsa Studio 同域托管（可选）

在调用方 Web 项目中额外安装：

```xml
<PackageReference Include="BioTrace.Elsa.Abp.Studio.BlazorWasm" Version="1.0.0" />
<PackageReference Include="BioTrace.Elsa.Abp.Studio.AspNetCore" Version="1.0.0" />
```

调用方需自有 Blazor WASM Client 项目引用 `Studio.BlazorWasm`（演示见 [`src/BioTrace.Elsa.Abp.Studio.Client`](../src/BioTrace.Elsa.Abp.Studio.Client/)，该壳**不**随 NuGet 发布）。

### Elsa EF 持久化（必填）

`BioTrace.Elsa.Abp.AspNetCore` 仅引用 `Elsa.Persistence.EFCore`（无 Provider）。调用方必须：

1. 引用 `Elsa.Persistence.EFCore.{PostgreSql|SqlServer|Sqlite}`（版本与 [Elsa 3.7.0](https://www.nuget.org/packages/Elsa) 成套）。
2. 创建继承 `ElsaAbpAspNetCoreModule` 的模块，重写 `ConfigureElsaPersistence`。
3. 在调用方 `[DependsOn]` 中使用**该自定义模块**（不要仅依赖 `ElsaAbpAspNetCoreModule`）。

PostgreSQL 示例（与演示项目 [`ElsaAbpHostPostgreSqlModule`](../host/BioTrace.Elsa.Abp.HttpApi.Host/ElsaAbpHostPostgreSqlModule.cs) 一致）：

```csharp
using Elsa.Extensions;
using Elsa.Features.Services;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Persistence.EFCore.Modules.Runtime;
using Volo.Abp.Modularity;

namespace MyApp;

public class MyAppElsaPostgreSqlModule : ElsaAbpAspNetCoreModule
{
    protected override void ConfigureElsaPersistence(IModule elsa, ElsaAbpOptions options)
    {
        elsa.UseWorkflowManagement(management =>
        {
            management.UseEntityFrameworkCore(ef =>
            {
                ef.UsePostgreSql(ResolveElsaConnectionString);
                ef.RunMigrations = options.RunMigrations;
            });
        });

        elsa.UseWorkflowRuntime(runtime =>
        {
            runtime.UseEntityFrameworkCore(ef =>
            {
                ef.UsePostgreSql(ResolveElsaConnectionString);
                ef.RunMigrations = options.RunMigrations;
            });
        });
    }
}
```

多租户时，自定义 Elsa 模块应 **`[DependsOn(typeof(ElsaAbpMultiTenancyModule))]`**（或确保 `ElsaAbpMultiTenancyModule` 先于 Elsa 模块加载），以便 `Elsa:EnableMultiTenancy` 在 `AddElsa` 之前生效。

### 其他可选包

| 场景 | 包 |
|---|---|
| 前端/其他服务调用 ABP API 代理 | `BioTrace.Elsa.Abp.HttpApi.Client` |
| ABP CLI / Studio 安装模块元数据 | `BioTrace.Elsa.Abp.Installer`（安装命令：`abp add-module BioTrace.Elsa.Abp` 或 Studio **Install module**） |
| 仅需 DTO/权限常量（类库） | `BioTrace.Elsa.Abp.Application.Contracts` |
| **多租户**（ABP TenantManagement + Elsa 行级隔离） | `ElsaAbpMultiTenancyModule`（在 `BioTrace.Elsa.Abp.AspNetCore` 包内）+ ABP TenantManagement 包（见下文） |

### 调用方还需的 ABP 官方包（安全与数据）

本模块**不包含** Identity / OpenIddict / PermissionManagement 的 EF 实现，调用方需自行引用（版本与 ABP 对齐，当前为 10.4.0）：

```xml
<PackageReference Include="Volo.Abp.Identity.EntityFrameworkCore" Version="10.4.0" />
<PackageReference Include="Volo.Abp.OpenIddict.EntityFrameworkCore" Version="10.4.0" />
<PackageReference Include="Volo.Abp.PermissionManagement.EntityFrameworkCore" Version="10.4.0" />
<PackageReference Include="Volo.Abp.OpenIddict.AspNetCore" Version="10.4.0" />
<PackageReference Include="Volo.Abp.PermissionManagement.Application" Version="10.4.0" />
<PackageReference Include="Volo.Abp.PermissionManagement.Domain.Identity" Version="10.4.0" />
<PackageReference Include="Volo.Abp.EntityFrameworkCore.PostgreSql" Version="10.4.0" />
```

若需登录页、Account 模块，可参考演示项目 额外引用 `Volo.Abp.Account.*`、`Volo.Abp.Identity.AspNetCore` 等。

### 多租户（可选）

启用 ABP + Elsa 共享库多租户时，调用方还需：

```xml
<PackageReference Include="Volo.Abp.TenantManagement.EntityFrameworkCore" Version="10.4.0" />
<PackageReference Include="Volo.Abp.TenantManagement.Application" Version="10.4.0" />
```

并在 `[DependsOn]` 中增加 `ElsaAbpMultiTenancyModule`、`AbpTenantManagementEntityFrameworkCoreModule`、`AbpTenantManagementApplicationModule`（完整示例见演示项目）。

## 模块依赖（`[DependsOn]`）

在调用方启动模块（如 `MyAppHttpApiHostModule`）上添加本模块与调用方既有模块。下例假设调用方已配置 Identity / OpenIddict / Permission 的 EF 模块：

```csharp
[DependsOn(
    typeof(AbpHttpApiModule),
    typeof(MyAppElsaPostgreSqlModule),              // 继承 ElsaAbpAspNetCoreModule + Provider
    typeof(ElsaAbpMultiTenancyModule),              // 可选：多租户
    // 以下为调用方既有 ABP 模块（非本仓库 EntityFrameworkCore 包）
    typeof(AbpEntityFrameworkCorePostgreSqlModule),
    typeof(AbpIdentityEntityFrameworkCoreModule),
    typeof(AbpOpenIddictEntityFrameworkCoreModule),
    typeof(AbpPermissionManagementEntityFrameworkCoreModule),
    typeof(AbpOpenIddictAspNetCoreModule),
    typeof(AbpPermissionManagementApplicationModule),
    typeof(AbpPermissionManagementDomainIdentityModule))]
public class MyAppHttpApiHostModule : AbpModule
{
    // ...
}
```

托管 Elsa Studio 时另加 `ElsaAbpStudioAspNetCoreModule` 及调用方 WASM Client 模块（见演示项目）。

## 配置文件（`appsettings.json`）

完整示例见 [`docs/appsettings.consumer.example.json`](appsettings.consumer.example.json)。核心结构如下。

### 双库连接串（必填）

ABP 业务库与 Elsa 工作流库**必须分离**：

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=MyApp_Abp;Username=app_user;Password=***",
    "Elsa": "Host=localhost;Port=5432;Database=MyApp_Elsa;Username=elsa_user;Password=***"
  }
}
```

- `Default`：对应调用方 ABP 标准 `Default` 连接名，存放 Identity、OpenIddict、PermissionManagement 等**调用方既有**表（本模块无自有业务表）。
- `Elsa`：对应 `ElsaAbpDbProperties.ConnectionStringName`（默认名 `Elsa`），存放 Elsa Management / Runtime 表。
- 未配置 `ConnectionStrings:Elsa` 时启动抛出 `Abp:ElsaConnectionStringNotConfigured`。

### Elsa 模块选项

```json
{
  "Elsa": {
    "EnableWorkflowsApi": true,
    "EnableElsaSwagger": false,
    "EnableHttpActivities": true,
    "EnablePermissionClaimsBridge": true,
    "DisableElsaEndpointSecurity": false
  }
}
```

| 配置项 | 说明 | 生产建议 |
|---|---|---|
| `EnableWorkflowsApi` | 暴露 `/elsa/api/*` | `true` |
| `EnableElsaSwagger` | 独立 FastEndpoints OpenAPI（`/swagger/elsa`） | `false` |
| `EnableHttpActivities` | 启用 Elsa HTTP 触发 Activity | 按需 |
| `EnablePermissionClaimsBridge` | 将 ABP 权限映射为 JWT/Principal 上的 `permissions` Claim | `true` |
| `DisableElsaEndpointSecurity` | 关闭 Elsa 端点鉴权（仅开发） | **禁止** `true` |
| `EnableMultiTenancy` | 启用 `Elsa.UseTenants` + ABP 租户桥接 | 多租户调用方设为 `true` |
| `HostTenantId` | ABP Host 映射到的 Elsa 租户 ID | 默认 `""` |
| `TenantIdClaimType` | 从 JWT 解析租户的 Claim 类型 | 默认 `tenantid` |

选项类型定义见 [`ElsaAbpOptions`](../src/BioTrace.Elsa.Abp.Domain/ElsaAbpOptions.cs)。

### 多租户配置（可选）

与 ABP `MultiTenancy:IsEnabled` 及 Domain.Shared 中 `MultiTenancyConsts.IsEnabled` 对齐：

```json
{
  "MultiTenancy": { "IsEnabled": true },
  "Elsa": {
    "EnableMultiTenancy": true
  }
}
```

- Elsa 仍使用**单一** `ConnectionStrings:Elsa`；工作流定义/实例按 `TenantId` 列隔离。
- 请求需传递 **`__tenant` Header**（租户名，如 `tenant-a`）或依赖 JWT **`tenantid` Claim**。
- **无需**为 Elsa 配置 per-tenant 连接串；租户主数据以 ABP `TenantManagement` 为准。

### 认证与 CORS

```json
{
  "App": {
    "SelfUrl": "https://api.mycompany.com",
    "CorsOrigins": ["https://studio.mycompany.com"],
    "RedirectAllowedUrls": "https://studio.mycompany.com"
  },
  "AuthServer": {
    "Authority": "https://api.mycompany.com",
    "SwaggerClientId": "MyApp_Swagger"
  }
}
```

- Elsa API 与 ABP API 共用 OpenIddict 签发的 **Bearer JWT**。
- CORS 须**显式 Origin** 且 `AllowCredentials()`，禁止 `AllowAnyOrigin()` 与 OIDC 混用。

## 数据库准备

### 1. 创建数据库与用户

PostgreSQL 示例（可按环境调整权限最小化）：

```sql
CREATE USER app_user WITH PASSWORD 'your_password';
CREATE USER elsa_user WITH PASSWORD 'your_password';

CREATE DATABASE "MyApp_Abp" OWNER app_user;
CREATE DATABASE "MyApp_Elsa" OWNER elsa_user;

GRANT ALL PRIVILEGES ON DATABASE "MyApp_Abp" TO app_user;
GRANT ALL PRIVILEGES ON DATABASE "MyApp_Elsa" TO elsa_user;
```

本地开发可参考 [`docker/postgres/init/01-create-databases.sql`](../docker/postgres/init/01-create-databases.sql) 一键创建演示库。

### 2. ABP 库迁移（Identity / OpenIddict / Permission）

在调用方**自有** EF 项目中为 Identity、OpenIddict、PermissionManagement 等 DbContext 维护迁移并执行（本模块不要求额外的业务 DbContext）。

演示项目 将 `AbpIdentity`、`AbpOpenIddict`、`AbpPermissionManagement` 连接串指向 **同一 `Default` 库**：

```csharp
Configure<AbpDbConnectionOptions>(options =>
{
    var defaultCs = configuration.GetConnectionString(AbpDbProperties.ConnectionStringName)!;
    options.ConnectionStrings.Default = defaultCs;
    options.ConnectionStrings["AbpIdentity"] = defaultCs;
    options.ConnectionStrings["AbpPermissionManagement"] = defaultCs;
    options.ConnectionStrings["AbpOpenIddict"] = defaultCs;
});
```

Elsa 工作流库的表结构与版本升级由调用方按 [Elsa 官方文档](https://elsaworkflows.io/) 自行维护；本模块不提供 Elsa 升级迁移指南。

## 权限配置

### 权限模型

本模块提供两套权限名称，通过 `ElsaAbpPermissionClaimsPrincipalContributor` 在每次请求时桥接：

| 层级 | 用途 | 定义位置 |
|---|---|---|
| **ABP 权限** | 角色/用户授权、管理界面、审计 | `AbpElsaPermissions` |
| **Elsa API Claim** | FastEndpoints 校验 `permissions` Claim | `ElsaApiPermissionNames` |

授予 `Abp.Elsa.Admin` 时映射为 Elsa 通配符 `*`；其余权限按 [`ElsaAbpPermissionMapper`](../src/BioTrace.Elsa.Abp.AspNetCore/Security/ElsaAbpPermissionMapper.cs) 逐项映射。

### ABP 权限清单（节选）

| ABP 权限 | 说明 |
|---|---|
| `Abp.Elsa.Admin` | 全部 Elsa API 权限（`*`） |
| `Abp.Elsa.NotReadOnly` | 绕过工作流定义只读策略 |
| `Abp.Elsa.WorkflowDefinitions.Read` | 读定义 |
| `Abp.Elsa.WorkflowDefinitions.Write` | 写定义 |
| `Abp.Elsa.WorkflowInstances.Execute` | 执行实例 |
| `Abp.Elsa.WorkflowInstances.Cancel` | 取消实例 |

完整列表见 [`AbpElsaPermissions`](../src/BioTrace.Elsa.Abp.Application.Contracts/Permissions/AbpElsaPermissions.cs) 与 [`AbpElsaPermissionDefinitionProvider`](../src/BioTrace.Elsa.Abp.Application.Contracts/Permissions/AbpElsaPermissionDefinitionProvider.cs)。

### 为角色授予权限

在调用方 `IDataSeedContributor` 或管理界面中为角色授权，示例（与演示项目一致）：

```csharp
await _permissionManager.SetForRoleAsync("admin", AbpElsaPermissions.Admin, true);
await _permissionManager.SetForRoleAsync("admin", AbpElsaPermissions.NotReadOnly, true);

await _permissionManager.SetForRoleAsync("designer",
    AbpElsaPermissions.WorkflowDefinitions.Read, true);
await _permissionManager.SetForRoleAsync("designer",
    AbpElsaPermissions.WorkflowInstances.Read, true);

await _permissionManager.SetForRoleAsync("operator",
    AbpElsaPermissions.WorkflowDefinitions.Read, true);
await _permissionManager.SetForRoleAsync("operator",
    AbpElsaPermissions.WorkflowInstances.Read, true);
await _permissionManager.SetForRoleAsync("operator",
    AbpElsaPermissions.WorkflowInstances.Execute, true);
await _permissionManager.SetForRoleAsync("operator",
    AbpElsaPermissions.WorkflowInstances.Cancel, true);
```

演示种子实现：[`ElsaAbpHostDataSeedContributor`](../host/BioTrace.Elsa.Abp.HttpApi.Host/Data/ElsaAbpHostDataSeedContributor.cs)。

### 查询当前用户 Elsa 权限（Studio / 前端）

不要仅解析 JWT 字符串判断按钮权限；调用模块 API 获取**最新**有效权限：

- `GET /api/abp/elsa/current-user`
- 兼容别名：`GET /identity/users/me`

返回体含 `permissions` 数组（Elsa Claim 值，如 `read:workflow-definitions`）。

### ABP ↔ Elsa Claim 对照（节选）

| ABP 权限 | Elsa Claim |
|---|---|
| `Abp.Elsa.Admin` | `*` |
| `Abp.Elsa.WorkflowDefinitions.Read` | `read:workflow-definitions` |
| `Abp.Elsa.WorkflowDefinitions.Write` | `write:workflow-definitions` |
| `Abp.Elsa.WorkflowInstances.Execute` | `execute:workflow-instances` |

## 调用方代码配置

### OpenIddict 校验（PreConfigure）

```csharp
PreConfigure<OpenIddictBuilder>(builder =>
{
    builder.AddValidation(options =>
    {
        options.AddAudiences("MyApp"); // 与资源服务器 Audience 一致
        options.UseLocalServer();
        options.UseAspNetCore();
    });
});

PreConfigure<OpenIddictServerBuilder>(serverBuilder =>
{
    serverBuilder.SetIssuer(new Uri(configuration["AuthServer:Authority"]!.TrimEnd('/')));
});
```

### 启用动态 Claims（推荐）

```csharp
Configure<AbpClaimsPrincipalFactoryOptions>(options =>
{
    options.IsDynamicClaimsEnabled = true;
});
```

### 管道中间件顺序

```csharp
public override void OnApplicationInitialization(ApplicationInitializationContext context)
{
    var app = context.GetApplicationBuilder();

    app.UseRouting();
    app.UseCors();
    app.UseAuthentication();
    app.UseAbpOpenIddictValidation();
    app.UseMultiTenancy();              // 多租户：在 Authorization 之前
    app.UseAuthorization();

    // Elsa OpenAPI 须在 Swashbuckle 之前（若启用 EnableElsaSwagger）
    app.UseElsaAbpMultiTenancy();       // 多租户：将 ABP 租户推入 Elsa ITenantAccessor
    app.UseElsaWorkflows();

    app.UseSwagger();
    app.UseAbpSwaggerUI(/* ... */);
    app.UseConfiguredEndpoints();
}
```

`UseElsaWorkflows()` 定义于 [`ElsaAbpApplicationBuilderExtensions`](../src/BioTrace.Elsa.Abp.AspNetCore/ElsaAbpApplicationBuilderExtensions.cs)。

### 注册自定义 Activity（可选）

继承 `ElsaAbpAspNetCoreModule` 并重写：

```csharp
public class MyAppElsaModule : ElsaAbpAspNetCoreModule
{
    protected override void ConfigureElsaActivities(IModule elsa)
    {
        base.ConfigureElsaActivities(elsa);
        elsa.AddActivity<MyCustomActivity>();
    }
}
```

## 验证集成

1. 启动调用方，确认 Elsa 持久化与连接串配置正确。
2. 使用具备 `Abp.Elsa.Admin` 的用户获取 access_token。
3. 调用 `GET /elsa/api/workflow-definitions`，应返回 200。
4. 使用仅 `WorkflowDefinitions.Read` 的用户尝试 `POST /elsa/api/workflow-definitions`，应返回 403。
5. 调用 `GET /api/abp/elsa/current-user`，确认 `permissions` 与角色一致。

集成测矩阵（T0–T8）见 [`test/BioTrace.Elsa.Abp.HttpApi.Host.Tests`](../test/BioTrace.Elsa.Abp.HttpApi.Host.Tests/)。

## Elsa Studio 集成（可选）

### 自有 Blazor WASM 客户端项目

引用 `BioTrace.Elsa.Abp.Studio.BlazorWasm`，在 `Program.cs` 中：

```csharp
builder.AddBioTraceElsaAbpStudio();
var app = builder.Build();
await app.RunBioTraceElsaAbpStudioAsync();
```

在 `wwwroot/appsettings.json` 配置 `ElsaStudio` 节（`Authentication:OpenIdConnect`、`Backend`、`Tenancy:Tenants`、`AbpApi:CurrentUserPath`）。完整示例见演示 [`Studio.Client`](../src/BioTrace.Elsa.Abp.Studio.Client/wwwroot/appsettings.json)。

### 调用方 API 内嵌 Hosted WASM（同域 `/studio`）

1. 引用 `BioTrace.Elsa.Abp.Studio.AspNetCore` 与 WASM Client 项目（可参考 [`Studio.Client`](../src/BioTrace.Elsa.Abp.Studio.Client/)）。
2. 调用方模块中注册：

```csharp
context.Services.AddBioTraceElsaAbpStudioHost(configuration);

// OnApplicationInitialization，在 UseConfiguredEndpoints 之前：
app.UseBioTraceElsaAbpStudioHost();
```

3. 配置 `ElsaStudio:Enabled`、`ElsaStudio:PathBase`（默认 `/studio`）。
4. OpenIddict 客户端 `ElsaStudio` 的 `RedirectUris` 须与 `PathBase` 对齐（如 `https://api.example.com/studio/authentication/login-callback`）。

RCL 已内置 `AbpTenantHeaderDelegatingHandler`、租户下拉 UI 与 current-user 权限查询；配置 `ElsaStudio:Tenancy:Tenants` 供根租户（Host）用户切换租户。

## 常见问题

| 现象 | 处理 |
|---|---|
| 启动报 `ElsaPersistenceNotConfigured` | 引用 `Elsa.Persistence.EFCore.{Provider}` 并继承 `ElsaAbpAspNetCoreModule` 重写 `ConfigureElsaPersistence` |
| 启动报 `ElsaConnectionStringNotConfigured` | 在 `ConnectionStrings` 中添加名为 `Elsa` 的连接串 |
| Elsa API 始终 401 | 检查 `UseAbpOpenIddictValidation`、JWT Audience、请求头 `Authorization: Bearer ...` |
| Elsa API 403 但 ABP 权限已授予 | 确认 `EnablePermissionClaimsBridge=true`；检查是否授予的是 `Abp.Elsa.*` 而非仅业务权限 |
| Studio 登录后无按钮 | 调用 `current-user` API 而非解析 JWT；检查 CORS 与 OpenIddict 客户端 RedirectUri |
| 双 Swagger 路径冲突 | 保持 `UseElsaWorkflows()` 在 `UseSwagger()` **之前** |
| 租户用户看不到 Elsa 数据 / 串租户 | 确认 `EnableMultiTenancy=true`、`UseMultiTenancy()` 与 `UseElsaAbpMultiTenancy()` 顺序；API 携带 `__tenant` Header |
| Elsa Studio 多租户 | 引用 `BioTrace.Elsa.Abp.Studio.BlazorWasm` 并调用 `AddBioTraceElsaAbpStudio()`；配置 `ElsaStudio:Tenancy:Tenants` 供根租户（Host）用户切换；Hosted 模式另需 `AddBioTraceElsaAbpStudioHost()` |
| Studio OIDC redirect 失败 | 确认 OpenIddict `RedirectUris` 与 `ElsaStudio:PathBase` 一致；开发环境重启调用方应用触发 Seed 更新 |

## 相关文档

- [README 调用方集成清单](../README.md#调用方集成清单)
- [README 安全集成](../README.md#安全集成abp-openiddict--elsa-370)
- [NuGet 发布准备](../README.md#nuget-发布准备)
