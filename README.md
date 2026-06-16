# BioTrace.Elsa.Abp

基于 [ABP Framework](https://abp.io/) 的可复用模块，**仅**解决在既有 ABP 调用方应用中集成 [Elsa Workflows](https://elsaworkflows.io/) 3.7 与 Elsa Studio（OpenIddict 权限桥接、多租户、同域 Hosted WASM）。**不包含**本模块自有业务实体；ABP 业务库由调用方既有 Identity / OpenIddict / Permission 等模块维护。

## 文档

| 文档 | 说明 |
|---|---|
| [调用方集成指南（NuGet）](docs/nuget-consumer-guide.md) | 其他项目引用 NuGet 包、配置双库、权限与 OpenIddict 的完整教程 |
| [Installer / `abp add-module`](#通过-abp-cli--abp-studio-安装) | 本模块安装器行为与安装后必做步骤 |
| [调用方配置示例](docs/appsettings.consumer.example.json) | 调用方 `appsettings.json` 模板（连接串、Elsa 选项、CORS、OpenIddict 客户端） |
| [Elsa 配置片段](docs/appsettings.elsa.json) / [Studio 配置片段](docs/appsettings.elsa.studio.json) | 可合并的 Elsa / OpenIddict / Studio 配置；配合 [`scripts/merge-appsettings-elsa.sh`](scripts/merge-appsettings-elsa.sh) |
| [Nexus / LeptonXLite 主题](docs/ui-theme-nexus.md) | UI 主题与 Studio OIDC、租户登录差异 |
| [Elsa 升级检查清单](docs/elsa-upgrade-checklist.md) | 小版本升级自检（非官方迁移指南） |
| [集成测试样板](docs/samples/HttpApi.Host.Tests/README.md) | 调用方 `HttpApi.Host.Tests` 最小示例 |
| [CHANGELOG](CHANGELOG.md) | NuGet 版本变更记录 |
| [贡献指南](CONTRIBUTING.md) | 开发、测试与 PR 流程 |

## 技术栈

- .NET 10
- ABP 10.4
- Entity Framework Core
- [Elsa Workflows](https://elsaworkflows.io/) **3.7.0**（原生集成，非 ABP Elsa Pro）
- Entity Framework Core 持久化（Provider 由调用方自选；演示项目使用 PostgreSQL）

> `BioTrace.Elsa.Abp.AspNetCore` **不**捆绑数据库 Provider；调用方需引用 `Elsa.Persistence.EFCore.{PostgreSql|SqlServer|Sqlite}` 并重写 `ConfigureElsaPersistence`（演示见 `ElsaAbpHostPostgreSqlModule`）。Elsa 表结构迁移见 [Elsa 官方文档](https://elsaworkflows.io/)；升级自检见 [Elsa 升级检查清单](docs/elsa-upgrade-checklist.md)。

## 解决方案结构

```
src/
  BioTrace.Elsa.Abp.Domain.Shared
  BioTrace.Elsa.Abp.Domain
  BioTrace.Elsa.Abp.Application.Contracts
  BioTrace.Elsa.Abp.Application          # 含示例 Activity（PrintMessageActivity）
  BioTrace.Elsa.Abp.EntityFrameworkCore  # 演示 Host / 单元测用 ABP DbContext（不发布 NuGet）
  BioTrace.Elsa.Abp.AspNetCore           # Elsa 集成入口（连接名 Elsa；不含 EF Provider）
  BioTrace.Elsa.Abp.HttpApi
  BioTrace.Elsa.Abp.HttpApi.Client
  BioTrace.Elsa.Abp.Installer
  BioTrace.Elsa.Abp.Studio.BlazorWasm    # Elsa Studio ABP 集成 RCL（租户/权限/组件）
  BioTrace.Elsa.Abp.Studio.AspNetCore    # Hosted WASM 服务端扩展
  BioTrace.Elsa.Abp.Studio.Client        # 演示用 Studio WASM 壳三文件样板（不发布 NuGet；可用 scaffold 脚本生成）
host/                                    # 仅本地验证，不发布 NuGet
  BioTrace.Elsa.Abp.HttpApi.Host         # 集成演示与 E2E（含 /studio）
  BioTrace.Elsa.Abp.DbMigrator           # 生产迁移样板（ABP 库 → Elsa force 迁移）
test/
  BioTrace.Elsa.Abp.TestBase
  BioTrace.Elsa.Abp.AspNetCore.Tests
  BioTrace.Elsa.Abp.Application.Tests
  BioTrace.Elsa.Abp.Domain.Tests
  BioTrace.Elsa.Abp.EntityFrameworkCore.Tests
  BioTrace.Elsa.Abp.HttpApi.Host.Tests   # 演示项目集成测（Contributor + OpenIddict + Elsa API）
  BioTrace.Elsa.Abp.E2E                  # Playwright 浏览器 E2E（npm，不在 slnx）
```

> **DbMigrator 连接串**：[`host/BioTrace.Elsa.Abp.DbMigrator`](host/BioTrace.Elsa.Abp.DbMigrator/) 示例库名为 `BioTraceElsaAbp` / `BioTraceElsaAbp_Elsa`，与演示 Host 的 `BioTrace_Abp` / `BioTrace_Elsa` **不同**——复制配置时需改库名。生产环境建议 `Elsa:RunMigrations=false`，由 DbMigrator 调用 `MigrateElsaDatabasesAsync(force: true)`。

## 调用方集成清单

> 从 NuGet 引用时的逐步教程见 **[调用方集成指南](docs/nuget-consumer-guide.md)**；配置模板见 **[appsettings.consumer.example.json](docs/appsettings.consumer.example.json)**。若偏好 CLI，可先执行 [`abp add-module BioTrace.Elsa.Abp`](#通过-abp-cli--abp-studio-安装) 添加包与 `[DependsOn]`，再完成下文手动步骤。

引用本模块的 ABP 应用**必须**单独配置 Elsa 连接串与持久化；Elsa 工作流库与 ABP 业务库分离维护。

1. 在调用方启动模块上添加依赖（**含 EF Provider 模块**，见 [调用方指南](docs/nuget-consumer-guide.md)）：
   ```csharp
   [DependsOn(typeof(MyAppElsaPostgreSqlModule), typeof(AbpHttpApiModule))]
   // MyAppElsaPostgreSqlModule 继承 ElsaAbpAspNetCoreModule 并重写 ConfigureElsaPersistence
   ```
2. 在 `appsettings.json` 中配置**独立**连接串（`Default` 为 ABP 业务库，`Elsa` 为工作流库）与 `Elsa` 选项（节选）：
   ```json
   {
     "ConnectionStrings": {
       "Default": "Host=...;Database=your_abp_db;...",
       "Elsa": "Host=...;Database=your_elsa_db;..."
     },
     "Elsa": {
       "RunMigrations": true,
       "EnableWorkflowsApi": true,
       "EnableElsaSwagger": true,
       "EnableHttpActivities": true,
       "EnablePermissionClaimsBridge": true
     }
   }
   ```
3. 在调用方 `OnApplicationInitialization` 中映射 Elsa 中间件（可调用扩展方法 `app.UseElsaWorkflows()`）。
4. （可选）在调用方模块中继承 `ElsaAbpAspNetCoreModule`，重写 `ConfigureElsaPersistence`（Provider）与 `ConfigureElsaActivities`（Activity）。
5. Elsa 工作流数据由 Elsa 持久化层维护，**不会**写入调用方 ABP 业务 DbContext。

未配置 `ConnectionStrings:Elsa` 时，启动将抛出 `Abp:ElsaConnectionStringNotConfigured`；未重写 `ConfigureElsaPersistence` 时抛出 `Abp:ElsaPersistenceNotConfigured`。

### `ElsaAbpOptions` 常用选项

完整说明见 [调用方集成指南](docs/nuget-consumer-guide.md)。

| 属性 | 默认值 | 说明 |
|------|--------|------|
| `RunMigrations` | `true` | 注册启动时 Elsa 库迁移；Provider 应设 `ef.RunMigrations = false` |
| `MigrateOnlyInDevelopment` | `true` | 生产设为 `false` 或改用 DbMigrator |
| `DeferTenantActivationUntilReady` | `false` | DbMigrator / K8s 冷启动建议 `true` |
| `EnableWorkflowsApi` | `true` | 启用 `/elsa/api/*` |
| `EnableElsaSwagger` | 开发环境 `true` | FastEndpoints OpenAPI：`/swagger/elsa` |
| `EnableHttpActivities` | `true` | `elsa.UseHttp()` |
| `EnablePermissionClaimsBridge` | `true` | ABP 权限 → JWT `permissions` Claim |
| `DisableElsaEndpointSecurity` | `false` | 仅 Development 可禁用 Elsa 端点安全 |
| `EnableMultiTenancy` | `false` | 引用 `ElsaAbpMultiTenancyModule` 时自动为 `true` |

## 通过 ABP CLI / ABP Studio 安装

调用方在既有 ABP 解决方案根目录执行：

```bash
abp add-module BioTrace.Elsa.Abp
```

或在 ABP Studio 中 **Install module** → 选择 `BioTrace.Elsa.Abp`。安装元数据由 `BioTrace.Elsa.Abp.Installer` 提供（`BioTrace.Elsa.Abp.abpmdl` 与各 `.abppkg`）。

> ABP CLI / Studio 本身的安装与升级请参阅 [ABP 官方文档](https://abp.io/docs/latest/cli)。本节仅说明**本模块**安装器会做什么、不会做什么。

安装器会下载 `BioTrace.Elsa.Abp.Installer`，按 `.abpmdl` 与各包 `.abppkg` 的 `role` 向对应层项目添加 NuGet 引用，并在启动模块写入 `[DependsOn(...)]`（典型包括 `BioTrace.Elsa.Abp.AspNetCore`、`BioTrace.Elsa.Abp.HttpApi` 及传递依赖；Studio 相关包按项目角色挂载）。

> **注意**：安装器**不会**添加 `BioTrace.Elsa.Abp.EntityFrameworkCore`（仅本仓库演示 Host，不发布 NuGet）、`Elsa.Persistence.EFCore.{Provider}`，也不会代为配置 Identity / OpenIddict / Permission——见 [调用方集成指南](docs/nuget-consumer-guide.md)。

### 安装器自动完成 vs 调用方手动配置

| 步骤 | 安装器 | 调用方手动 |
|------|--------|------------|
| 添加本模块 NuGet 与 `[DependsOn]` | ✅ | — |
| 引用 `Elsa.Persistence.EFCore.{Provider}` | ❌ | ✅ 自选 Provider |
| 继承 `ElsaAbpAspNetCoreModule` 并重写 `ConfigureElsaPersistence` | ❌ | ✅ |
| 配置 `ConnectionStrings:Default` 与 `ConnectionStrings:Elsa` | ❌ | ✅ |
| Host 管道：`UseMultiTenancy()` → `UseElsaAbpMultiTenancy()` → `UseElsaWorkflows()` | ❌ | ✅ |
| 角色授予 `Abp.Elsa.*` 权限 | ❌ | ✅ |
| Elsa Studio（可选）：`ElsaStudio` / OpenIddict 客户端与 WASM 注册 | ❌ | ✅ |

完整步骤与配置模板见 **[调用方集成指南](docs/nuget-consumer-guide.md)** 与 **[appsettings.consumer.example.json](docs/appsettings.consumer.example.json)**。安装包内说明见 [`InstallationNotes.md`](src/BioTrace.Elsa.Abp.Installer/InstallationNotes.md)。

## 安全集成（ABP OpenIddict ↔ Elsa 3.7.0）

本模块**不启用** `Elsa.Identity`，由调用方 **OpenIddict** 签发单一 JWT，同时保护 ABP API 与 Elsa Workflows API（FastEndpoints 校验 Principal 上的 `permissions` Claim）。

### 架构要点

- **ABP 权限**（Permission Management）：`Abp.Elsa.*`，用于角色/用户授权与审计。
- **Elsa 运行时权限**（API Claim）：`read:workflow-definitions` 等，由 `ElsaAbpPermissionClaimsPrincipalContributor` 在请求时从 ABP 权限映射注入。
- **单一真相源**：`IElsaAbpEffectivePermissionsProvider` 供 Claims 桥接与 Studio 当前用户 API 共用。
- **Studio 按钮权限**：勿仅解析 JWT；调用 `GET /api/abp/elsa/current-user`（别名 `GET /identity/users/me`）获取最新 `permissions` 数组。

开发环境（`ASPNETCORE_ENVIRONMENT=Development`）演示项目 `Program.cs` 在 `InitializeApplicationAsync` 之后依次执行：

1. **`ElsaAbpHostDatabaseMigrationHostedService`** — 迁移 ABP 业务库（`AbpDbContext`、Identity、OpenIddict、Permission、TenantManagement）
2. **`MigrateElsaDatabasesAsync`** — 迁移 Elsa 工作流库（由 `Elsa:RunMigrations` 控制）
3. **`IDataSeeder`** — 种子数据（Host `admin`、OpenIddict 客户端、租户与用户等）
4. **`ElsaAbpTenantDemoWorkflowSeeder`** — 演示租户工作流定义

### OpenIddict / CORS / Elsa Studio

`appsettings.json` 示例（端口与 `launchSettings.json` 一致）：

```json
{
  "App": {
    "CorsOrigins": ["https://localhost:44388"]
  },
  "AuthServer": {
    "Authority": "https://localhost:44388",
    "SwaggerClientId": "BioTrace_Elsa_Abp_Swagger"
  },
  "OpenIddict": {
    "Applications": {
      "ElsaStudio": {
        "ClientId": "ElsaStudio",
        "RootUrl": "https://localhost:44388/studio",
        "RedirectUris": [
          "https://localhost:44388/studio/authentication/login-callback"
        ]
      }
    }
  },
  "ElsaStudio": {
    "Enabled": true,
    "PathBase": "/studio"
  },
  "Elsa": {
    "EnableElsaSwagger": true,
    "EnablePermissionClaimsBridge": true,
    "DisableElsaEndpointSecurity": false
  }
}
```

- CORS 必须**显式 Origin** + `AllowCredentials()`，禁止 `AllowAnyOrigin()` 与 OIDC 混用。
- **双 Swagger**（ABP Swashbuckle 与 Elsa FastEndpoints 分离，见下节）。
- Elsa Studio 由 HttpApi.Host 同域托管于 `/studio`；Code Flow 客户端 `ElsaStudio`。
- 生产环境勿设置 `Elsa:DisableElsaEndpointSecurity=true`；生产建议 `Elsa:EnableElsaSwagger=false`。

### 双 Swagger（ABP API + Elsa Workflows API）

| 文档 | OpenAPI JSON | UI |
|------|----------------|-----|
| BioTrace.Elsa.Abp API | `https://localhost:44388/swagger/v1/swagger.json` | `https://localhost:44388/swagger`（OAuth2） |
| Elsa Workflows API | `https://localhost:44388/swagger/elsa/openapi.json` | `https://localhost:44388/swagger/elsa`，或在 ABP UI 下拉选择 **Elsa Workflows API** |

- ABP 文档：OAuth2 Authorization Code，ClientId `BioTrace_Elsa_Abp_Swagger`，Scope `BioTrace_Elsa_Abp`。
- Elsa 文档：由 `FastEndpoints.Swagger` 生成，需 **Bearer JWT**（不支持 ABP Swagger 的 OAuth 一键授权）。
- 在 ABP Swagger 用 OAuth 登录后，将 `access_token` 填入 Elsa Swagger 的 **Bearer** 字段即可试用 API。

Elsa API 校验的是请求时由 `ElsaAbpPermissionClaimsPrincipalContributor` 注入的 **`permissions` Claim**（如 `read:workflow-definitions`），不是 ABP 权限名字符串。ABP Swagger 的 `GET /api/abp/elsa/current-user` 仍用于查看当前用户的 Elsa 权限列表。

### ABP ↔ Elsa 权限对照（节选）

| ABP 权限 | Elsa Claim / 行为 |
|----------|-------------------|
| `Abp.Elsa.Admin` | `*` |
| `Abp.Elsa.WorkflowDefinitions.Read` | `read:workflow-definitions` |
| `Abp.Elsa.WorkflowDefinitions.Write` | `write:workflow-definitions` |
| `Abp.Elsa.WorkflowDefinitions.Publish` | `publish:workflow-definitions` |
| `Abp.Elsa.WorkflowInstances.Execute` | `execute:workflow-instances` |
| `Abp.Elsa.HttpEndpoints.Invoke` | HTTP Trigger 走 ABP 权限（`AbpHttpEndpointAuthorizationHandler`，非 Claim） |
| `Abp.Elsa.NotReadOnly` | 满足 Elsa `NotReadOnlyRequirement`（`AbpElsaNotReadOnlyAuthorizationHandler` 扩展，非 Claim） |

完整常量见 `AbpElsaPermissions` 与 `ElsaApiPermissionNames`（`Application.Contracts`）。

## 多租户（ABP + Elsa 共享库）

本模块可选启用 **ABP 多租户 + Elsa `Elsa.Tenants` 行级隔离**（单一 `ConnectionStrings:Elsa`，按 `TenantId` 列过滤，无需每租户独立 Elsa 连接串）。若调用方**不需要**多租户，勿引用 `ElsaAbpMultiTenancyModule`。

### 架构要点

- **租户主数据**：ABP `TenantManagement`（`AbpTenants`）；**不**启用 Elsa 自带 Tenant CRUD API。
- **上下文桥接**：`ICurrentTenant` → `ElsaAbpTenantMapper`（Host → `""`，租户 → `Guid.ToString("D")`）→ Elsa `ITenantAccessor`。
- **可选模块**：`ElsaAbpMultiTenancyModule`（调用方按需引用）。
- **管道顺序**：`UseAuthentication` → `UseAbpOpenIddictValidation` → **`UseMultiTenancy()`** → `UseAuthorization` → **`UseElsaAbpMultiTenancy()`** → `UseElsaWorkflows()`。

### 配置示例

```json
{
  "MultiTenancy": { "IsEnabled": true },
  "ConnectionStrings": {
    "Default": "Host=...;Database=BioTrace_Abp;...",
    "Elsa": "Host=...;Database=BioTrace_Elsa;..."
  },
  "Elsa": {
    "EnableMultiTenancy": true,
    "DeferTenantActivationUntilReady": false
  }
}
```

`MultiTenancyConsts.IsEnabled`（Domain.Shared）当前**编译为 `true`**；调用方 `MultiTenancy:IsEnabled` 配置须与之匹配。API / Studio 请求需携带 ABP 标准 **`__tenant` Header**（或 Query），或依赖 JWT 内 **`tenantid` Claim**（OpenIddict 自动写入）。

Host 级 `admin` 默认只见 **Host 租户**（`TenantId` 为空）下的工作流；在 Elsa Studio 右上角租户下拉中选择 `Tenant A` 后，出站请求会自动附加 `__tenant: tenant-a`。租户用户（如 `tenant-a-admin`）登录后自动锁定所属租户并附带同名 Header。

> **常见现象**
> - 若流程是在 Host 上下文（未选租户）下创建的，其 `TenantId` 为空，仅 `admin` 在 Host 视图下可见；租户用户列表为空。请切换到目标租户后再创建，或依赖演示种子工作流。
> - 租户用户通过 OIDC 登录时，须在调用方登录页填写/选择租户（或访问 `https://localhost:44388/Account/Login?__tenant=tenant-a`），否则 Token 可能缺少 `tenantid` Claim。
> - 切换登录账户后 Studio 会强制刷新页面；若仍显示上一用户数据，请先 Logout 再登录。

## 演示项目快速开始

`host/BioTrace.Elsa.Abp.HttpApi.Host` 为本地验证与 CI 测试用，**不随 NuGet 发布**。除本模块外，它还引用 Identity / OpenIddict / PermissionManagement EF 模块；在**既有**调用方应用中集成时，沿用调用方已有模块即可。

Elsa Server 与 Studio 前端均为 **3.7.0**（见根目录 `Directory.Build.props` 中 `ElsaPackageVersion`）。

```bash
docker compose up -d
# 首次或更新 Basic Theme 依赖后：安装演示项目前端库（登录页 /Account/Login 需要）
cd host/BioTrace.Elsa.Abp.HttpApi.Host && npm install && cd ../..
dotnet run --project host/BioTrace.Elsa.Abp.HttpApi.Host
```

演示项目已种子 OpenIddict 公共客户端 `ElsaStudio`（Authorization Code + PKCE）。Studio 由 **HttpApi.Host 同域 Hosted WASM** 提供，路径 `/studio`，OIDC 跳转至 `/Account/Login`（Basic Theme）。

| 地址 | 说明 |
|------|------|
| [https://localhost:44388/studio](https://localhost:44388/studio) | Elsa Studio（勿在末尾多加 `/`） |
| [https://localhost:44388/swagger](https://localhost:44388/swagger) | ABP Swagger |
| [https://localhost:44388/swagger/elsa](https://localhost:44388/swagger/elsa) | Elsa Swagger |
| `https://localhost:44388/elsa/api/*` | Elsa Workflows API |

- WASM 运行时配置：`src/BioTrace.Elsa.Abp.Studio.Client/wwwroot/appsettings.json`
- Host 配置：`host/BioTrace.Elsa.Abp.HttpApi.Host/appsettings.json` 中 `ElsaStudio` 与 `OpenIddict:Applications:ElsaStudio`
- `docker/postgres/init` 会创建 `BioTrace_Abp`、`BioTrace_Elsa` 及测试/E2E 用库共六个库
- VS Code：**F5** → **Launch HttpApi.Host (HTTPS)**

### 开发种子账户（密码均为 `1q2w3E*`）

| 租户 | 用户 | 角色 / 说明 |
|------|------|-------------|
| Host | `admin` | Host 级 `admin`；`Abp.Elsa.Admin` → Elsa `*` |
| `tenant-a` | `tenant-a-admin` | 租户内 `admin`，写/执行/取消等 Elsa 权限 |
| `tenant-a` | `tenant-a-designer` | 租户内 `designer`，只读 |
| `tenant-b` | `tenant-b-admin` | 租户内 `admin`，用于隔离验证 |
| `tenant-b` | `tenant-b-designer` | 租户内 `designer`，只读 |

## NuGet 包

调用方如何安装包、配置数据库与权限，见 **[调用方集成指南](docs/nuget-consumer-guide.md)**。维护者发布流程见 **[docs/release.md](docs/release.md)**。

### 调用方直接引用（典型）

| 场景 | 在调用方项目中显式引用的包 |
|------|--------------------------|
| Elsa Workflows API + 权限桥接 | `BioTrace.Elsa.Abp.AspNetCore`、`BioTrace.Elsa.Abp.HttpApi` |
| Elsa EF 持久化 | 调用方自选 `Elsa.Persistence.EFCore.{PostgreSql\|SqlServer\|Sqlite}`（3.7.0） |
| 同域托管 Elsa Studio | `BioTrace.Elsa.Abp.Studio.BlazorWasm`、`BioTrace.Elsa.Abp.Studio.AspNetCore` |

`AspNetCore` 会传递依赖 `Application`、`Domain`、`Application.Contracts`、`Domain.Shared`，一般无需逐个安装。调用方**已有** ABP Identity / OpenIddict / Permission 的 EF 模块即可，**无需**引用本仓库的 `EntityFrameworkCore` 项目。

### 将会发布的包

| 包名 | 说明 |
|------|------|
| `BioTrace.Elsa.Abp.AspNetCore` | **集成入口**（Elsa 注册、权限桥接、多租户） |
| `BioTrace.Elsa.Abp.HttpApi` | `current-user` 等 ABP API |
| `BioTrace.Elsa.Abp.Studio.BlazorWasm` | Studio UI 与 ABP 租户/权限组件 |
| `BioTrace.Elsa.Abp.Studio.AspNetCore` | 调用方内嵌 `/studio` 托管扩展（不含 WASM 壳） |
| `BioTrace.Elsa.Abp.Application` | 示例 Activity 等（`AspNetCore` 传递） |
| `BioTrace.Elsa.Abp.Application.Contracts` | 权限常量、DTO（传递或单独引用） |
| `BioTrace.Elsa.Abp.Domain` / `Domain.Shared` | 选项与常量（传递） |
| `BioTrace.Elsa.Abp.HttpApi.Client` | 可选：动态 API 代理 |
| `BioTrace.Elsa.Abp.Installer` | 可选：ABP CLI / Studio 安装元数据 |
| `BioTrace.Elsa.Abp.IntegrationTesting` | 可选：集成测试 WebApplicationFactory 与 OpenIddict 辅助 |

## 许可证

本项目采用 [MIT License](LICENSE) 开源。
