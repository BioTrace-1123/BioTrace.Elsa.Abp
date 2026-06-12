# BioTrace.Elsa.Abp

基于 [ABP Framework](https://abp.io/) 的可复用模块，**仅**解决在既有 ABP 调用方应用中集成 [Elsa Workflows](https://elsaworkflows.io/) 3.7 与 Elsa Studio（OpenIddict 权限桥接、多租户、同域 Hosted WASM）。**不包含**本模块自有业务实体；ABP 业务库由调用方既有 Identity / OpenIddict / Permission 等模块维护。

## 文档

| 文档 | 说明 |
|---|---|
| [调用方集成指南（NuGet）](docs/nuget-consumer-guide.md) | 其他项目引用 NuGet 包、配置双库、权限与 OpenIddict 的完整教程 |
| [Installer / `abp add-module`](#通过-abp-cli--abp-studio-安装) | 本模块安装器行为与安装后必做步骤 |
| [调用方配置示例](docs/appsettings.consumer.example.json) | 调用方 `appsettings.json` 模板（连接串、Elsa 选项、CORS、OpenIddict 客户端） |
| [贡献指南](CONTRIBUTING.md) | Git Flow 与 PR 流程 |
| [浏览器 E2E（Playwright）](#浏览器-e2eplaywright) | Studio OIDC 全链路、多租户与权限的自动化验证 |

## 技术栈

- .NET 10
- ABP 10.4
- Entity Framework Core
- [Elsa Workflows](https://elsaworkflows.io/) **3.7.0**（原生集成，非 ABP Elsa Pro）
- Entity Framework Core 持久化（Provider 由调用方自选；演示项目使用 PostgreSQL）

> `BioTrace.Elsa.Abp.AspNetCore` **不**捆绑数据库 Provider；调用方需引用 `Elsa.Persistence.EFCore.{PostgreSql|SqlServer|Sqlite}` 并重写 `ConfigureElsaPersistence`（演示见 `ElsaAbpHostPostgreSqlModule`）。Elsa 版本升级与库表结构变更由调用方参照 [Elsa 官方文档](https://elsaworkflows.io/) 自行处理，本模块不提供升级迁移指南。

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
  BioTrace.Elsa.Abp.Studio.Client        # 演示用 Studio WASM 壳（不单独发布 NuGet）
host/                                    # 仅本地验证，不发布 NuGet
  BioTrace.Elsa.Abp.HttpApi.Host         # 集成演示与 E2E（含 /studio）
test/
  BioTrace.Elsa.Abp.TestBase
  BioTrace.Elsa.Abp.AspNetCore.Tests
  BioTrace.Elsa.Abp.Application.Tests
  BioTrace.Elsa.Abp.Domain.Tests
  BioTrace.Elsa.Abp.EntityFrameworkCore.Tests
  BioTrace.Elsa.Abp.HttpApi.Host.Tests   # 演示项目集成测（Contributor + OpenIddict + Elsa API）
  BioTrace.Elsa.Abp.E2E                  # Playwright 浏览器 E2E（npm，不在 slnx）
```

## 调用方集成清单

> 从 NuGet 引用时的逐步教程见 **[调用方集成指南](docs/nuget-consumer-guide.md)**；配置模板见 **[appsettings.consumer.example.json](docs/appsettings.consumer.example.json)**。若偏好 CLI，可先执行 [`abp add-module BioTrace.Elsa.Abp`](#通过-abp-cli--abp-studio-安装) 添加包与 `[DependsOn]`，再完成下文手动步骤。

引用本模块的 ABP 应用**必须**单独配置 Elsa 连接串与持久化；Elsa 工作流库与 ABP 业务库分离维护。

1. 在调用方启动模块上添加依赖（**含 EF Provider 模块**，见 [调用方指南](docs/nuget-consumer-guide.md)）：
   ```csharp
   [DependsOn(typeof(MyAppElsaPostgreSqlModule), typeof(AbpHttpApiModule))]
   // MyAppElsaPostgreSqlModule 继承 ElsaAbpAspNetCoreModule 并重写 ConfigureElsaPersistence
   ```
2. 在 `appsettings.json` 中配置**独立**连接串（`Default` 为 ABP 业务库，`Elsa` 为工作流库）：
   ```json
   {
     "ConnectionStrings": {
       "Default": "Host=...;Database=your_abp_db;...",
       "Elsa": "Host=...;Database=your_elsa_db;..."
     },
     "Elsa": {
       "EnableWorkflowsApi": true,
       "EnableElsaSwagger": true,
       "EnableHttpActivities": true
     }
   }
   ```
3. 在调用方 `OnApplicationInitialization` 中映射 Elsa 中间件（可调用扩展方法 `app.UseElsaWorkflows()`）。
4. （可选）在调用方模块中继承 `ElsaAbpAspNetCoreModule`，重写 `ConfigureElsaPersistence`（Provider）与 `ConfigureElsaActivities`（Activity）。
5. Elsa 工作流数据由 Elsa 持久化层维护，**不会**写入调用方 ABP 业务 DbContext。

未配置 `ConnectionStrings:Elsa` 时，启动将抛出 `Abp:ElsaConnectionStringNotConfigured`；未重写 `ConfigureElsaPersistence` 时抛出 `Abp:ElsaPersistenceNotConfigured`。

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

修改 `.abpmdl` / `.abppkg` 或 Installer 后，请在调用方测试环境验证 `abp add-module BioTrace.Elsa.Abp` 是否仍正确挂载包与依赖。

## 安全集成（ABP OpenIddict ↔ Elsa 3.7.0）

本模块**不启用** `Elsa.Identity`，由调用方 **OpenIddict** 签发单一 JWT，同时保护 ABP API 与 Elsa Workflows API（FastEndpoints 校验 Principal 上的 `permissions` Claim）。

### 架构要点

- **ABP 权限**（Permission Management）：`Abp.Elsa.*`，用于角色/用户授权与审计。
- **Elsa 运行时权限**（API Claim）：`read:workflow-definitions` 等，由 `ElsaAbpPermissionClaimsPrincipalContributor` 在请求时从 ABP 权限映射注入。
- **单一真相源**：`IElsaAbpEffectivePermissionsProvider` 供 Claims 桥接与 Studio 当前用户 API 共用。
- **Studio 按钮权限**：勿仅解析 JWT；调用 `GET /api/abp/elsa/current-user`（别名 `GET /identity/users/me`）获取最新 `permissions` 数组。

### 演示账户（开发种子）

| 用户 | 密码 | 角色 | 说明 |
|------|------|------|------|
| `admin` | `1q2w3E*` | admin | `Abp.Elsa.Admin` → Elsa `*` |

租户演示账户见下文「多租户」；只读/执行场景请使用 `tenant-a-designer` / `tenant-a-admin`（非 Host 级 `designer`/`operator`）。

开发环境演示项目启动时会自动 **Migrate + Seed**（`ElsaAbpHostDatabaseMigrationHostedService`，仅 Development）。

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
- Elsa Studio 由 HttpApi.Host 同域托管于 `/studio`（见下文「运行 Elsa Studio」）；Code Flow 客户端 `ElsaStudio`。
- 生产环境勿设置 `Elsa:DisableElsaEndpointSecurity=true`；生产建议 `Elsa:EnableElsaSwagger=false`。

### 双 Swagger（ABP API + Elsa Workflows API）

| 文档 | OpenAPI JSON | UI |
|------|----------------|-----|
| BioTrace.Elsa.Abp API | `https://localhost:44388/swagger/v1/swagger.json` | `https://localhost:44388/swagger`（OAuth2） |
| Elsa Workflows API | `https://localhost:44388/swagger/elsa/openapi.json` | `https://localhost:44388/swagger/elsa`，或在 ABP UI 下拉选择 **Elsa Workflows API** |

- ABP 文档：OAuth2 Authorization Code，ClientId `BioTrace_Elsa_Abp_Swagger`，Scope `BioTrace_Elsa_Abp`。
- Elsa 文档：由 `FastEndpoints.Swagger` 生成，需 **Bearer JWT**（不支持 ABP Swagger 的 OAuth 一键授权）。
- 开关：`Elsa:EnableElsaSwagger`（与 `EnableWorkflowsApi` 联动；演示项目 开发环境默认 `true`）。

**在 Elsa Swagger 中调用 API（获取 Token）**

1. 打开 `https://localhost:44388/swagger`，点击 **Authorize**，用 OAuth 登录（如 `admin` / `1q2w3E*`）；亦可直接访问 `https://localhost:44388/Account/Login`（Basic Theme 登录页）。
2. 亦可先 `POST /api/account/login` 再 Authorize，或对 `/connect/token` 使用已配置的客户端。
3. 从 OAuth 对话框或浏览器网络面板复制 **access_token**。
4. 打开 `https://localhost:44388/swagger/elsa`（或在 ABP Swagger 下拉切换到 Elsa 文档），在 **Bearer** 中填入：`Bearer {access_token}`。
5. 试用 `GET /elsa/api/workflow-definitions`：`admin` 返回 200；`tenant-a-designer`（租户只读）返回 200；无写权限的变更操作返回 403。

Elsa API 校验的是请求时由 `ElsaAbpPermissionClaimsPrincipalContributor` 注入的 **`permissions` Claim**（如 `read:workflow-definitions`），不是 ABP 权限名字符串。ABP Swagger 的 `GET /api/abp/elsa/current-user` 仍用于查看当前用户的 Elsa 权限列表。

### ABP ↔ Elsa 权限对照（节选）

| ABP 权限 | Elsa Claim |
|----------|------------|
| `Abp.Elsa.Admin` | `*` |
| `Abp.Elsa.WorkflowDefinitions.Read` | `read:workflow-definitions` |
| `Abp.Elsa.WorkflowDefinitions.Write` | `write:workflow-definitions` |
| `Abp.Elsa.WorkflowDefinitions.Publish` | `publish:workflow-definitions` |
| `Abp.Elsa.WorkflowInstances.Execute` | `execute:workflow-instances` |
| `Abp.Elsa.NotReadOnly` | 满足 ASP.NET `NotReadOnlyPolicy`（非 Claim） |

完整常量见 `AbpElsaPermissions` 与 `ElsaApiPermissionNames`（`Application.Contracts`）。

## 多租户（ABP + Elsa 共享库）

本模块可选启用 **ABP 多租户 + Elsa `Elsa.Tenants` 行级隔离**（单一 `ConnectionStrings:Elsa`，按 `TenantId` 列过滤，无需每租户独立 Elsa 连接串）。

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
    "EnableMultiTenancy": true
  }
}
```

`MultiTenancyConsts.IsEnabled`（Domain.Shared）与上述配置应对齐。API / Studio 请求需携带 ABP 标准 **`__tenant` Header**（或 Query），或依赖 JWT 内 **`tenantid` Claim**（OpenIddict 自动写入）。

### 演示租户（开发种子）

| 租户 | 用户 | 密码 | 说明 |
|------|------|------|------|
| `tenant-a` | `tenant-a-admin` | `1q2w3E*` | 租户管理员，含 Elsa 写权限 |
| `tenant-a` | `tenant-a-designer` | `1q2w3E*` | 租户只读 designer |
| `tenant-b` | `tenant-b-admin` | `1q2w3E*` | 用于隔离验证 |
| Host | `admin` | `1q2w3E*` | Host 管理员（`Abp.Elsa.Admin` → `*`） |

Host 级 `admin` 默认只见 **Host 租户**（`TenantId` 为空）下的工作流；在 Elsa Studio 右上角租户下拉中选择 `Tenant A` 后，出站请求会自动附加 `__tenant: tenant-a`，此时应只看到 Tenant A 的流程（`ElsaAbpMultiTenancyModule` 在 ABP 租户解析链最前为 Host 用户启用 `__tenant` 头覆盖，避免 `CurrentUser` 解析抢先锁定 Host 上下文）。租户用户（如 `tenant-a-admin`）登录后自动锁定所属租户并附带同名 Header。

> **常见现象**
> - 若流程是在 Host 上下文（未选租户）下创建的，其 `TenantId` 为空，仅 `admin` 在 Host 视图下可见；租户用户列表为空。请切换到目标租户后再创建，或依赖下方演示种子工作流。
> - 租户用户通过 OIDC 登录时，须在调用方登录页填写/选择租户（或访问 `https://localhost:44388/Account/Login?__tenant=tenant-a`），否则 Token 可能缺少 `tenantid` Claim，导致权限与工作流隔离失效。
> - 切换登录账户后 Studio 会强制刷新页面；若仍显示上一用户数据，请先 Logout 再登录。

### 演示项目 说明（不随 NuGet 发布）

`host/BioTrace.Elsa.Abp.HttpApi.Host` 为本地验证与 CI 测试用。除本模块外，它还引用调用方常见的 Identity / OpenIddict / PermissionManagement EF 模块，并将 `AbpIdentity`、`AbpOpenIddict`、`AbpPermissionManagement` 连接串指向同一 `ConnectionStrings:Default` 库（与 Elsa 库仍分离）。在**既有**调用方应用中集成时，沿用调用方已有模块即可，无需复制演示项目的全部 DependsOn。

## 本地运行演示项目（PostgreSQL）

```bash
docker compose up -d
# 首次或更新 Basic Theme 依赖后：安装演示项目前端库（登录页 /Account/Login 需要）
cd host/BioTrace.Elsa.Abp.HttpApi.Host && npm install && cd ../..
dotnet run --project host/BioTrace.Elsa.Abp.HttpApi.Host
```

- **演示入口**（演示项目启动后手动在浏览器打开）：

  | 地址 | 说明 |
  |------|------|
  | [https://localhost:44388/studio](https://localhost:44388/studio) | Elsa Studio |
  | [https://localhost:44388/swagger](https://localhost:44388/swagger) | ABP Swagger |
  | [https://localhost:44388/swagger/elsa](https://localhost:44388/swagger/elsa) | Elsa Swagger |

- Elsa Workflows API：`https://localhost:44388/elsa/api/*`（如 `workflow-definitions`）
- `docker/postgres/init` 会创建 `BioTrace_Abp`、`BioTrace_Elsa`、`BioTrace_Abp_Test`、`BioTrace_Elsa_Test`、`BioTrace_Abp_E2E`、`BioTrace_Elsa_E2E` 六个库

## 运行 Elsa Studio

演示项目已种子 OpenIddict 公共客户端 `ElsaStudio`（Authorization Code + PKCE）。Studio 由 **HttpApi.Host 同域 Hosted WASM** 提供，路径为 `/studio`，通过 OpenId Connect 向调用方 API 换 Token，再调用 Elsa Workflows API。

> **版本说明**：Elsa Server 与 Studio 前端均为 **3.7.0**（版本由仓库根目录 `Directory.Build.props` 中 `ElsaPackageVersion` 集中管理）。

**前置**：PostgreSQL 已启动（见上一节）。

```bash
dotnet run --project host/BioTrace.Elsa.Abp.HttpApi.Host
```

- Studio UI：[https://localhost:44388/studio](https://localhost:44388/studio)（同域 Hosted WASM，勿在末尾多加 `/` 以免部分环境出现重定向循环）
- WASM 运行时配置：`src/BioTrace.Elsa.Abp.Studio.Client/wwwroot/appsettings.json`（`ElsaStudio:Backend`、`ElsaStudio:Authentication:OpenIdConnect`、`ElsaStudio:Tenancy:Tenants`）
- 演示项目配置：`host/BioTrace.Elsa.Abp.HttpApi.Host/appsettings.json` 中 `ElsaStudio:Enabled`、`ElsaStudio:PathBase` 与 `OpenIddict:Applications:ElsaStudio`
- 登录：OIDC 跳转至调用方 `/Account/Login`（Basic Theme）；使用演示账户如 `admin` / `1q2w3E*`（需具备相应 `Abp.Elsa.*` 权限）
- **多租户**：`BioTrace.Elsa.Abp.Studio.BlazorWasm` 通过 `AbpTenantHeaderDelegatingHandler` 向 Elsa API 与 `identity/users/me` 附加 ABP 标准 `__tenant` Header。Host `admin` 可在右上角下拉切换 `Host` / `Tenant A` / `Tenant B`；租户用户显示只读租户标签。切换后页面会强制刷新以重载工作流列表。
- VS Code：**F5** 选择 **Launch HttpApi.Host (HTTPS)**，启动后手动打开上表中的演示入口

## Dev Container（可选）

仓库提供 [Dev Containers](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers) 配置（`.devcontainer/`），内含 .NET 10 SDK、Node.js、PostgreSQL 与 Playwright 依赖。

**前置**：[Docker](https://docs.docker.com/get-docker/) 与 VS Code [Dev Containers 扩展](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers)。

1. 克隆仓库并在 VS Code 中打开根目录。
2. 命令面板执行 **Dev Containers: Reopen in Container**。
3. 等待 `postCreateCommand` 完成（`dotnet restore`、HTTPS 开发证书、演示项目前端依赖等）。
4. **F5** → **Launch HttpApi.Host (HTTPS)**，在浏览器打开 `/studio`、`/swagger`、`/swagger/elsa`。

容器内 PostgreSQL 主机为 Compose 服务名 `postgres`（见 `devcontainer.json` 中的 `ConnectionStrings__*` 与 `INTEGRATION_TEST_POSTGRES_HOST`），无需在容器内单独执行 `docker compose up -d`。集成测与浏览器 E2E 可直接运行：

```bash
./scripts/test-integration.sh
./scripts/test-e2e.sh
# 或显式指定 Postgres 主机（与集成测相同）
E2E_POSTGRES_HOST=postgres ./scripts/test-e2e.sh
```

详见下文 [浏览器 E2E（Playwright）](#浏览器-e2eplaywright)。

| 现象 | 处理 |
|------|------|
| 端口 `5432` 冲突 | 停止本地 PostgreSQL，或修改根目录 `docker-compose.yml` 端口映射 |
| HTTPS 证书不受信任 | 容器内执行 `dotnet dev-certs https --trust` |
| 数据库未就绪 | 等待 Compose 中 `postgres` 健康检查通过后再启动演示项目 |

不使用 Dev Container 时，按上文「本地运行演示项目」在宿主机安装 .NET 10 并运行 `docker compose up -d`。

## 本地开发

**要求**： [.NET SDK 10](https://dotnet.microsoft.com/download)（见仓库根目录 `global.json`）

```bash
dotnet restore BioTrace.Elsa.Abp.slnx
dotnet build BioTrace.Elsa.Abp.slnx

# 单元测试（默认，不依赖 PostgreSQL）
dotnet test BioTrace.Elsa.Abp.slnx --filter "Category!=Integration"
```

### 测试分层

| 层级 | 项目 | 依赖 | 命令 |
|------|------|------|------|
| 单元测 | `BioTrace.Elsa.Abp.*.Tests`（含 Mapper/Provider/Contributor） | 无 Postgres | `dotnet test --filter "Category!=Integration"` |
| Contributor 快测 | [`AspNetCore.Tests`](test/BioTrace.Elsa.Abp.AspNetCore.Tests/) | 无 Postgres | 同上（`ElsaAbpPermissionClaimsPrincipalContributor_Tests`） |
| 演示项目集成测 | [`HttpApi.Host.Tests`](test/BioTrace.Elsa.Abp.HttpApi.Host.Tests/) | Postgres | [`./scripts/test-integration.sh`](scripts/test-integration.sh) |
| 浏览器 E2E | [`BioTrace.Elsa.Abp.E2E`](test/BioTrace.Elsa.Abp.E2E/) | Postgres + Playwright | [`./scripts/test-e2e.sh`](scripts/test-e2e.sh) |

### 演示项目集成测

项目 [`test/BioTrace.Elsa.Abp.HttpApi.Host.Tests`](test/BioTrace.Elsa.Abp.HttpApi.Host.Tests/) 通过 `WebApplicationFactory` + **Password Grant 测试客户端**（仅测试配置启用）验证 Contributor 桥接、OpenIddict 验签与 Elsa API 鉴权。

**测试客户端**（仅 WAF 注入，不在演示 `appsettings.json` 中）：

| 项 | 值 |
|----|-----|
| ClientId | `BioTrace_Elsa_Abp_IntegrationTests` |
| ClientSecret | `integration-test-secret` |
| 开关 | `AuthServer:AllowPasswordGrantForIntegrationTests=true` |

**用例矩阵（T0–T5）**

| # | 场景 | 断言 |
|---|------|------|
| T0 | 无 Bearer Token | `GET /elsa/api/workflow-definitions` → 401 |
| T1 | `tenant-a-designer` Password Grant Token | JWT `permissions` **无** `*` / `write:*`（只读用户 token 保持最小权限） |
| T2 | admin Password Grant Token | `GET /identity/users/me` → 200，`permissions` 含 `*` |
| T3 | admin Password Grant Token | `GET /elsa/api/workflow-definitions` → 200 |
| T4 | `tenant-a-designer` Token | `current-user` 仅 read claims |
| T5 | `tenant-a-designer` Token + `__tenant` | `POST /elsa/api/workflow-definitions` → 403 |
| T6 | 租户 A admin 创建定义 + 租户 B 列表 | 租户 B **不应**看到租户 A 的 `definitionId` |
| T7 | Host admin 列表 | **不应**看到租户内工作流定义 |
| T8 | 租户用户 Token | JWT 含 `tenantid` Claim |

**三种运行场景**

| 场景 | Postgres 主机 | 前置 | 命令 |
|------|---------------|------|------|
| 宿主机 | `localhost`（默认） | `docker compose up -d` | `./scripts/test-integration.sh` |
| Dev Container | `postgres`（环境变量已配置） | Compose `postgres` 服务健康即可 | `./scripts/test-integration.sh` |
| CI | `localhost`（GHA service） | 自动 | `integration-tests` job |

连接串主机可通过环境变量 `INTEGRATION_TEST_POSTGRES_HOST` 覆盖（默认 `localhost`）。测试库 `BioTrace_Abp_Test`、`BioTrace_Elsa_Test` 在 Postgres 可达时由测试 fixture 自动创建（亦见 [`docker/postgres/init/01-create-databases.sql`](docker/postgres/init/01-create-databases.sql)）。

### 浏览器 E2E（Playwright）

项目 [`test/BioTrace.Elsa.Abp.E2E`](test/BioTrace.Elsa.Abp.E2E/) 通过 **真实 OIDC Authorization Code + PKCE** 登录 Elsa Studio，验证 UI、多租户切换与工作流生命周期（创建/发布/执行/取消）。**不在** `BioTrace.Elsa.Abp.slnx` 内，使用独立 npm 包与 Playwright。

#### 前置条件

| 项 | 说明 |
|----|------|
| PostgreSQL | 宿主机：`docker compose up -d`；Dev Container：Compose `postgres` 服务已就绪 |
| Node.js | 与 Dev Container / CI 一致（E2E 目录 `npm ci`） |
| Playwright Chromium | `./scripts/test-e2e.sh` 会自动 `playwright install chromium`；Dev Container 在 `postCreateCommand` 中已预装 |
| HTTPS 证书 | 宿主机首次运行需 `dotnet dev-certs https --trust`（CI 与 Dev Container 已处理） |

E2E 使用独立库 `BioTrace_Abp_E2E`、`BioTrace_Elsa_E2E`（见 `docker/postgres/init`）。`playwright.config.ts` 的 `webServer` 会在每次运行前重建上述库并自动启动演示项目（Development Migrate + Seed），**无需**手动 `dotnet run` Host。

#### 一键运行

```bash
# 宿主机（先 docker compose up -d）
./scripts/test-e2e.sh

# Dev Container 内（Postgres 主机为 postgres）
E2E_POSTGRES_HOST=postgres ./scripts/test-e2e.sh
```

环境变量（可选）：

| 变量 | 默认 | 说明 |
|------|------|------|
| `E2E_POSTGRES_HOST` | `localhost`（或继承 `INTEGRATION_TEST_POSTGRES_HOST`） | PostgreSQL 主机 |
| `E2E_BASE_URL` | `https://localhost:44388` | 演示项目基址 |

VS Code 任务：**test-e2e**（等价于 `./scripts/test-e2e.sh`）。

#### 本地调试（进入 E2E 目录）

```bash
cd test/BioTrace.Elsa.Abp.E2E
npm ci
npx playwright install chromium

# 仍由 webServer 自动起 Host；仅跑单个 spec：
npx playwright test tests/studio-smoke.spec.ts

# 可视化调试
npm run test:e2e:ui
npm run test:e2e:headed

# 查看 HTML 报告
npm run show-report
```

#### 用例概览

| Spec | 场景 |
|------|------|
| `auth.setup.ts` | 为各角色执行 OIDC 登录并缓存 `storageState` |
| `studio-smoke.spec.ts` | `admin` 加载 Studio、打开工作流定义列表 |
| `studio-tenancy.spec.ts` | 租户 A/B 列表隔离；Host `admin` 切换租户后可见租户 A 演示流 |
| `studio-permissions.spec.ts` | `tenant-a-designer` 只读不可创建；`tenant-a-admin` 可创建 |
| `studio-workflow-lifecycle.spec.ts` | 创建 → 发布 → 执行 → 实例完成 |
| `studio-instance-cancel.spec.ts` | 运行中 Delay 工作流实例取消 |

**E2E 演示账户**（与种子一致）：

| 用户 | 密码 | 租户 | 用途 |
|------|------|------|------|
| `admin` | `1q2w3E*` | Host（根租户） | Studio 冒烟、根租户上下文代管 |
| `tenant-a-admin` | `1q2w3E*` | `tenant-a` | 工作流 CRUD/执行/取消 |
| `tenant-a-designer` | `1q2w3E*` | `tenant-a` | 只读权限 |
| `tenant-b-admin` | `1q2w3E*` | `tenant-b` | 租户隔离 |

CI：`e2e-tests` job（Postgres + `dotnet dev-certs https` + Playwright Chromium，`ignoreHTTPSErrors` 无需系统信任）；失败时上传 `playwright-report` 构件。

演示项目的 `appsettings.json` **未**启用 Password Grant；仅 WAF 注入 `AuthServer:AllowPasswordGrantForIntegrationTests=true` 时生效（集成测专用，E2E 走真实 OIDC）。

## 在 GitHub 上发布

1. 在 GitHub 创建空仓库（不要勾选 “Add a README”）。
2. 关联远程并推送 **两个长期分支**：

```bash
git remote add origin https://github.com/<org>/<repo>.git
git push -u origin main
git push -u origin develop
```

3. 在仓库 **Settings → General → Default branch** 将默认分支设为 **`develop`**（便于功能 PR）。
4. 为 `main`、`develop` 配置分支保护与必需 CI 检查（见 [CONTRIBUTING.md](CONTRIBUTING.md)）。

## CI

向 `main` / `develop` 的 **PR**（含 `feature/*`、`release/*`、`hotfix/*` 等源分支），以及合并后对 `main`、`develop` 的 **push** 会触发 [GitHub Actions](.github/workflows/ci.yml)（同一 PR 更新只跑一套，不重复触发 push + pull_request）：

| Job | 内容 |
|-----|------|
| `build-and-test` | 编译 + **单元测**（`Category!=Integration`，不依赖 Postgres） |
| `integration-tests` | **演示项目集成测**（GHA `postgres:15-alpine` service，T0–T8） |
| `e2e-tests` | **Playwright 浏览器 E2E**（Postgres + Chromium，OIDC + Studio 全链路） |

PR 合并前三个 job 均须通过。

## NuGet 发布准备

仓库已提供统一打包元数据（见 `common.props`）与发布工作流 [`.github/workflows/nuget-publish.yml`](.github/workflows/nuget-publish.yml)。**仅发布 Elsa / Studio 集成相关库**；`host/` 与 `test/` 不打包。

调用方如何安装包、配置数据库与权限，见 **[调用方集成指南](docs/nuget-consumer-guide.md)**。

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
| `BioTrace.Elsa.Abp.Studio.AspNetCore` | 调用方内嵌 `/studio` 扩展 |
| `BioTrace.Elsa.Abp.Application` | 示例 Activity 等（`AspNetCore` 传递） |
| `BioTrace.Elsa.Abp.Application.Contracts` | 权限常量、DTO（传递或单独引用） |
| `BioTrace.Elsa.Abp.Domain` / `Domain.Shared` | 选项与常量（传递） |
| `BioTrace.Elsa.Abp.HttpApi.Client` | 可选：动态 API 代理 |
| `BioTrace.Elsa.Abp.Installer` | 可选：ABP CLI / Studio 安装元数据（`abp add-module BioTrace.Elsa.Abp`） |

### 发布前检查清单

1. 版本号对齐：更新 `common.props` 的 `<Version>`（或发布时由 workflow 输入/Tag 覆盖）。
2. 通过 CI：确保编译、单元测、集成测全部通过。
3. README 完整：确认模块集成方式、连接串、权限说明与版本策略无误。
4. NuGet Trusted Publishing：在 [nuget.org Trusted Publishing](https://www.nuget.org/account/trustedpublishing) 配置策略（Repository Owner: `BioTrace-1123`，Repository: `BioTrace.Elsa.Abp`，Workflow: `nuget-publish.yml`，Package owner 与下方 Secret 一致）；在 GitHub 仓库 Secrets 设置 `NUGET_USER`（NuGet.org **用户名**，如 `a1mu`，非邮箱）。无需长期 `NUGET_API_KEY`。
5. 许可证：根目录 `LICENSE`（MIT），NuGet 元数据见 `common.props` 的 `PackageLicenseExpression`。

### 本地打包验证

```bash
dotnet restore BioTrace.Elsa.Abp.slnx
dotnet build BioTrace.Elsa.Abp.slnx -c Release

dotnet pack src/BioTrace.Elsa.Abp.AspNetCore/BioTrace.Elsa.Abp.AspNetCore.csproj \
  -c Release --no-build -o ./artifacts/nuget
```

需要一次性打全部包时，可按 `nuget-publish.yml` 中 `Pack module projects` 的列表逐个执行 `dotnet pack`。

### GitHub Actions 发布方式

支持两种触发方式：

- **手工触发**：`Actions -> NuGet Publish -> Run workflow`，输入版本号（如 `1.2.3`）。
- **Tag 触发**：推送 `v*.*.*`（如 `v1.2.3`）后自动发布。

工作流会先产出 `artifacts/nuget` 并上传构建产物，再通过 OIDC（`NuGet/login@v1`）换取短期密钥并执行 `dotnet nuget push --skip-duplicate`。

## 许可证

本项目采用 [MIT License](LICENSE) 开源。
