# BioTrace.Elsa.Abp

基于 [ABP Framework](https://abp.io/) 的可复用 **Application Module**，用于在宿主应用中集成 BioTrace / Elsa 相关能力。

## 文档

| 文档 | 说明 |
|---|---|
| [消费方集成指南（NuGet）](docs/nuget-consumer-guide.md) | 其他项目引用 NuGet 包、配置双库、权限与 OpenIddict 的完整教程 |
| [消费方配置示例](docs/appsettings.consumer.example.json) | 宿主 `appsettings.json` 模板（连接串、Elsa 选项、CORS、OpenIddict 客户端） |
| [贡献指南](CONTRIBUTING.md) | Git Flow 与 PR 流程 |

## 技术栈

- .NET 10
- ABP 10.4（DDD 模块模板）
- Entity Framework Core
- [Elsa Workflows](https://elsaworkflows.io/) **3.5.3**（原生集成，非 ABP Elsa Pro）
- PostgreSQL（Elsa 持久化；与 ABP 业务库分离）

> Elsa 核心包最新为 3.7.x，但 `Elsa.EntityFrameworkCore.PostgreSql` 目前最高为 **3.5.3**，故本模块统一锁定 **3.5.3** 以避免版本冲突。

## 解决方案结构

```
src/
  BioTrace.Elsa.Abp.Domain.Shared
  BioTrace.Elsa.Abp.Domain
  BioTrace.Elsa.Abp.Application.Contracts
  BioTrace.Elsa.Abp.Application          # 含示例 Activity（PrintMessageActivity）
  BioTrace.Elsa.Abp.EntityFrameworkCore  # ABP 业务 DbContext（连接名 Default）
  BioTrace.Elsa.Abp.AspNetCore           # Elsa 服务注册（连接名 Elsa）
  BioTrace.Elsa.Abp.HttpApi
  BioTrace.Elsa.Abp.HttpApi.Client
  BioTrace.Elsa.Abp.Installer
  BioTrace.Elsa.Abp.Studio.BlazorWasm    # Elsa Studio ABP 集成 RCL（租户/权限/组件）
  BioTrace.Elsa.Abp.Studio.Client        # Elsa Studio WASM 可执行壳（Hosted）
  BioTrace.Elsa.Abp.Studio.AspNetCore    # Hosted WASM 服务端扩展
host/
  BioTrace.Elsa.Abp.HttpApi.Host         # 本地验证宿主（含 /studio 托管 Elsa Studio）
test/
  BioTrace.Elsa.Abp.TestBase
  BioTrace.Elsa.Abp.AspNetCore.Tests
  BioTrace.Elsa.Abp.Application.Tests
  BioTrace.Elsa.Abp.Domain.Tests
  BioTrace.Elsa.Abp.EntityFrameworkCore.Tests
  BioTrace.Elsa.Abp.HttpApi.Host.Tests   # Host 集成测（Contributor + OpenIddict + Elsa API）
```

## 宿主集成清单

> 从 NuGet 引用时的逐步教程见 **[消费方集成指南](docs/nuget-consumer-guide.md)**；配置模板见 **[appsettings.consumer.example.json](docs/appsettings.consumer.example.json)**。

引用本模块的 ABP 应用**必须**单独配置 Elsa 数据库；仅引用 NuGet/项目**不会**自动创建 Elsa 表。

1. 在宿主启动模块上添加依赖：
   ```csharp
   [DependsOn(typeof(ElsaAbpAspNetCoreModule), typeof(AbpHttpApiModule))]
   ```
2. 在 `appsettings.json` 中配置**独立**连接串（`Default` 为 ABP 业务库，`Elsa` 为工作流库）：
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
       "EnableHttpActivities": true
     }
   }
   ```
3. 在宿主 `OnApplicationInitialization` 中映射 Elsa 中间件（可调用扩展方法 `app.UseElsaWorkflows()`）。
4. （可选）在宿主模块中重写 `ElsaAbpAspNetCoreModule` 的 `ConfigureElsa` / `ConfigureElsaActivities` 以注册更多 Activity。
5. Elsa 表由 Elsa EF 迁移维护，**不会**出现在 `AbpDbContext` 的迁移中。

未配置 `ConnectionStrings:Elsa` 时，启动将抛出 `Abp:ElsaConnectionStringNotConfigured` 业务异常。

## 安全集成（ABP OpenIddict ↔ Elsa 3.5.3）

本模块**不启用** `Elsa.Identity`，由宿主 **OpenIddict** 签发单一 JWT，同时保护 ABP API 与 Elsa Workflows API（FastEndpoints 校验 Principal 上的 `permissions` Claim）。

### 架构要点

- **ABP 权限**（Permission Management）：`Abp.Elsa.*`，用于角色/用户授权与审计。
- **Elsa 运行时权限**（API Claim）：`read:workflow-definitions` 等，由 `ElsaAbpPermissionClaimsPrincipalContributor` 在请求时从 ABP 权限映射注入。
- **单一真相源**：`IElsaAbpEffectivePermissionsProvider` 供 Claims 桥接与 Studio 当前用户 API 共用。
- **Studio 按钮权限**：勿仅解析 JWT；调用 `GET /api/abp/elsa/current-user`（别名 `GET /identity/users/me`）获取最新 `permissions` 数组。

### Host 演示账户（开发种子）

| 用户 | 密码 | 角色 | 说明 |
|------|------|------|------|
| `admin` | `1q2w3E*` | admin | `Abp.Elsa.Admin` → Elsa `*` |
| `designer` | `1q2w3E*` | designer | 只读 Definitions + Instances |
| `operator` | `1q2w3E*` | operator | 读定义/实例 + 执行/取消实例 |

开发环境 Host 启动时会自动 **Migrate + Seed**（`ElsaAbpHostDatabaseMigrationHostedService`，仅 Development）。

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
- 开关：`Elsa:EnableElsaSwagger`（与 `EnableWorkflowsApi` 联动；演示 Host 开发环境默认 `true`）。

**在 Elsa Swagger 中调用 API（获取 Token）**

1. 打开 `https://localhost:44388/swagger`，点击 **Authorize**，用 OAuth 登录（如 `admin` / `1q2w3E*`）；亦可直接访问 `https://localhost:44388/Account/Login`（Basic Theme 登录页）。
2. 亦可先 `POST /api/account/login` 再 Authorize，或对 `/connect/token` 使用已配置的客户端。
3. 从 OAuth 对话框或浏览器网络面板复制 **access_token**。
4. 打开 `https://localhost:44388/swagger/elsa`（或在 ABP Swagger 下拉切换到 Elsa 文档），在 **Bearer** 中填入：`Bearer {access_token}`。
5. 试用 `GET /elsa/api/workflow-definitions`：`admin` 返回 200；`designer` 只读 200；无写权限的变更操作返回 403。

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
- **可选模块**：`ElsaAbpMultiTenancyModule`（消费方按需引用）。
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
    "RunMigrations": true
  }
}
```

`MultiTenancyConsts.IsEnabled`（Domain.Shared）与上述配置应对齐。API / Studio 请求需携带 ABP 标准 **`__tenant` Header**（或 Query），或依赖 JWT 内 **`tenantid` Claim**（OpenIddict 自动写入）。

### Host 演示租户（开发种子）

| 租户 | 用户 | 密码 | 说明 |
|------|------|------|------|
| `tenant-a` | `tenant-a-admin` | `1q2w3E*` | 租户管理员，含 Elsa 写权限 |
| `tenant-a` | `tenant-a-designer` | `1q2w3E*` | 租户只读 designer |
| `tenant-b` | `tenant-b-admin` | `1q2w3E*` | 用于隔离验证 |
| Host | `admin` | `1q2w3E*` | Host 管理员（`Abp.Elsa.Admin` → `*`） |

Host 级 `admin` 默认只见 **Host 租户**（`TenantId` 为空）下的工作流；在 Elsa Studio 右上角租户下拉中选择 `Tenant A` 后，出站请求会自动附加 `__tenant: tenant-a`，此时应只看到 Tenant A 的流程（`ElsaAbpMultiTenancyModule` 在 ABP 租户解析链最前为 Host 用户启用 `__tenant` 头覆盖，避免 `CurrentUser` 解析抢先锁定 Host 上下文）。租户用户（如 `tenant-a-admin`）登录后自动锁定所属租户并附带同名 Header。

> **常见现象**
> - 若流程是在 Host 上下文（未选租户）下创建的，其 `TenantId` 为空，仅 `admin` 在 Host 视图下可见；租户用户列表为空。请切换到目标租户后再创建，或依赖下方演示种子工作流。
> - 租户用户通过 OIDC 登录时，须在 Host 登录页填写/选择租户（或访问 `https://localhost:44388/Account/Login?__tenant=tenant-a`），否则 Token 可能缺少 `tenantid` Claim，导致权限与工作流隔离失效。
> - 切换登录账户后 Studio 会强制刷新页面；若仍显示上一用户数据，请先 Logout 再登录。

### 宿主额外依赖（演示）

除 `ElsaAbpAspNetCoreModule` 外，演示 Host 引用 Identity / OpenIddict / PermissionManagement EF 模块，并将 `AbpIdentity`、`AbpOpenIddict`、`AbpPermissionManagement` 连接串指向同一 `ConnectionStrings:Default` 库（与 Elsa 库仍分离）。

## 本地运行 Host（PostgreSQL）

```bash
docker compose up -d
# 首次或更新 Basic Theme 依赖后：安装 Host 前端库（登录页 /Account/Login 需要）
cd host/BioTrace.Elsa.Abp.HttpApi.Host && npm install && cd ../..
dotnet run --project host/BioTrace.Elsa.Abp.HttpApi.Host
```

- ABP Swagger：`https://localhost:44388/swagger`
- Elsa Swagger：`https://localhost:44388/swagger/elsa`
- Elsa Workflows API：`https://localhost:44388/elsa/api/*`（如 `workflow-definitions`）
- `docker/postgres/init` 会创建 `BioTrace_Abp`、`BioTrace_Elsa`、`BioTrace_Abp_Test`、`BioTrace_Elsa_Test` 四个库

## 运行 Elsa Studio

演示宿主已种子 OpenIddict 公共客户端 `ElsaStudio`（Authorization Code + PKCE）。Studio 由 **HttpApi.Host 同域 Hosted WASM** 提供，路径为 `/studio`，通过 OpenId Connect 向 Host 换 Token，再调用 Elsa Workflows API。

> **版本说明**：Elsa Server 锁定 **3.5.3**；`Elsa.Studio.*` 前端使用 **3.7.0**（OpenIdConnect WASM 包自 3.7 起发布）。Api.Client 与 3.5.3 后端在演示环境中兼容。

**前置**：PostgreSQL 已启动（见上一节）。

```bash
dotnet run --project host/BioTrace.Elsa.Abp.HttpApi.Host
```

- Studio UI：`https://localhost:44388/studio`
- WASM 运行时配置：`src/BioTrace.Elsa.Abp.Studio.Client/wwwroot/appsettings.json`（`ElsaStudio:Backend`、`ElsaStudio:Authentication:OpenIdConnect`、`ElsaStudio:Tenancy:Tenants`）
- Host 托管配置：`host/BioTrace.Elsa.Abp.HttpApi.Host/appsettings.json` 中 `ElsaStudio:Enabled`、`ElsaStudio:PathBase` 与 `OpenIddict:Applications:ElsaStudio`
- 登录：OIDC 跳转至 Host `/Account/Login`（Basic Theme）；使用演示账户如 `admin` / `1q2w3E*`（需具备相应 `Abp.Elsa.*` 权限）
- **多租户**：`BioTrace.Elsa.Abp.Studio.BlazorWasm` 通过 `AbpTenantHeaderDelegatingHandler` 向 Elsa API 与 `identity/users/me` 附加 ABP 标准 `__tenant` Header。Host `admin` 可在右上角下拉切换 `Host` / `Tenant A` / `Tenant B`；租户用户显示只读租户标签。切换后页面会强制刷新以重载工作流列表。
- VS Code / Cursor：**F5** 选择 **Launch HttpApi.Host (HTTPS)**，浏览器自动打开 `/studio`

> 若从独立 Studio 端口（`:5003`）迁移，请重启 Host 以重新 Seed OpenIddict 客户端 RedirectUri，或手动更新 `OpenIddictApplications` 表。

## 使用 Dev Container（推荐）

克隆仓库后，可用 VS Code / Cursor 的 [Dev Containers](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers) 一键进入容器化开发环境（含 PostgreSQL 与 .NET 10 SDK）。

**前置**：安装 [Docker Desktop](https://www.docker.com/products/docker-desktop/)（启用 **WSL 2** 引擎），以及 Dev Containers 扩展。仓库在 WSL 内时，须在 Docker Desktop → **Settings → Resources → WSL integration** 中为 **Ubuntu** 打开集成，并在 WSL 终端能执行 `docker version`。

1. 打开仓库根目录（推荐：先用 **WSL: Connect to WSL** 打开 `/root/source/repos/BioTrace.Elsa.Abp`，再 **Reopen in Container**；避免仅从 Windows 侧打开 `\\wsl.localhost\...` 却未启用 WSL 集成）。
2. 命令面板执行 **Dev Containers: Reopen in Container**。
3. 等待镜像构建与 `postCreate`（`dotnet dev-certs https --trust`、`dotnet restore`）。
4. 按 **F5**，选择 **Launch HttpApi.Host (HTTPS)**。
5. 浏览器访问 `https://localhost:44388/studio`（Elsa Studio）或 `https://localhost:44388/swagger`（ABP API）。

容器内通过环境变量将数据库主机设为 Compose 服务名 `postgres`（`ConnectionStrings__Default` / `ConnectionStrings__Elsa`），不影响在宿主机上直接使用 `appsettings.json` 里的 `localhost` 连接串。集成测同样使用 `INTEGRATION_TEST_POSTGRES_HOST=postgres`（已在 devcontainer 配置），**无需在容器内再执行 `docker compose up -d`**；直接运行 `./scripts/test-integration.sh` 即可。

**常见问题**

| 现象 | 处理 |
|------|------|
| `Failed to install Cursor server` / `docker compose up` 失败；日志含 `ubuntu.sock: no such file or directory` 或 `distro mount service` | 在 Docker Desktop 为 **Ubuntu** 启用 WSL integration；PowerShell 执行 `wsl -d Ubuntu` 启动发行版后 `wsl --shutdown`，重启 Docker Desktop，再 **Reopen in Container**。仍失败则改为在 WSL 内打开项目，或将仓库克隆到 Windows 路径（如 `C:\dev\`）后重试 |
| WSL 内提示 `docker: command not found` | 同上，打开 Docker Desktop 的 WSL integration；不要只在 WSL 里装 `docker.io` 却未连上 Desktop |
| 宿主机 `5432` 已被占用 | 停止本地 PostgreSQL，或临时修改根目录 `docker-compose.yml` 的端口映射 |
| HTTPS 证书不受信任 | 在容器终端执行 `dotnet dev-certs https --trust` |
| 数据库未就绪 | 确认 `docker compose` 中 `postgres` 健康检查通过后再启动 Host |
| 不用容器、仅在宿主机开发 | 仍按上文「本地运行 Host」：`docker compose up -d` + `dotnet run` |

## 在 WSL 中使用 Cursor（性能与 Git 界面）

本仓库位于 WSL **ext4**（`/root/source/repos/...`），磁盘顺序读约 **2.3 GB/s**、4K 随机读 IOPS 约 **2.4 万**；若放在 Windows 盘（`/mnt/c`，9p）则慢约 **10–16 倍**，IDE 索引与 `git status` 会明显卡顿。**请保持仓库在 WSL 内，不要迁到 `C:\`。**

### 推荐打开方式（避免 Git 面板残留、Explorer 不刷新）

| 方式 | 说明 |
|------|------|
| **推荐** | 安装 [WSL](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-wsl) 扩展 → **WSL: Connect to WSL** → 打开 `/root/source/repos/BioTrace.Elsa.Abp` |
| **可选** | 在 WSL 终端执行 `cursor .`（在仓库根目录） |
| **不推荐** | 仅从 Windows 资源管理器打开 `\\wsl.localhost\Ubuntu\...` 且左下角**未**显示 `WSL: Ubuntu` — Git/文件监视易失效 |

命令行或 Agent 在**集成终端外**执行 `git commit` 后，Source Control 有时不会立刻更新（WSL2 对 `.git/index` 的 rename 监视不稳定）。处理：

1. 切回 Cursor 窗口（已启用 `git.refreshOnWindowFocus`）或命令面板 **Git: Refresh**
2. 仓库内已配置 `.vscode/settings.json`：`git.autorefresh`、`git.autofetch: false`（本仓库）、排除 `bin`/`obj` 监视
3. 若仍偶发不刷新：用户设置中临时设 `"remote.WSL.fileWatcher.polling": true` 后 **Reload Window**

### WSL 一次性调优（需 sudo）

```bash
# 提高 inotify 上限，避免大仓库监视耗尽
grep -q fs.inotify.max_user_watches /etc/sysctl.conf || \
  echo fs.inotify.max_user_watches=524288 | sudo tee -a /etc/sysctl.conf
sudo sysctl -p
```

可选：在 Windows 用户目录 `%UserProfile%\.wslconfig` 中限制 WSL 内存，避免与 Cursor 争抢（示例 `[wsl2] memory=8GB`）。

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
| Host 集成测 | [`HttpApi.Host.Tests`](test/BioTrace.Elsa.Abp.HttpApi.Host.Tests/) | Postgres | [`./scripts/test-integration.sh`](scripts/test-integration.sh) |

### Host 集成测

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
| T1 | designer Password Grant Token | JWT `permissions` **无** `*` / `write:*`（只读用户 token 保持最小权限） |
| T2 | admin Password Grant Token | `GET /identity/users/me` → 200，`permissions` 含 `*` |
| T3 | admin Password Grant Token | `GET /elsa/api/workflow-definitions` → 200 |
| T4 | designer Token | `current-user` 仅 read claims |
| T5 | designer Token + `__tenant` | `POST /elsa/api/workflow-definitions` → 403 |
| T6 | 租户 A admin 创建定义 + 租户 B 列表 | 租户 B **不应**看到租户 A 的 `definitionId` |
| T7 | Host admin 列表 | **不应**看到租户内工作流定义 |
| T8 | 租户用户 Token | JWT 含 `tenantid` Claim |

**三种运行场景**

| 场景 | Postgres 主机 | 前置 | 命令 |
|------|---------------|------|------|
| 宿主机 | `localhost`（默认） | `docker compose up -d` | `./scripts/test-integration.sh` |
| Dev Container | `postgres`（`INTEGRATION_TEST_POSTGRES_HOST` 已配置） | Compose `postgres` 服务健康即可 | `./scripts/test-integration.sh` |
| CI | `localhost`（GHA service） | 自动 | `integration-tests` job |

连接串主机可通过环境变量 `INTEGRATION_TEST_POSTGRES_HOST` 覆盖（默认 `localhost`）。测试库 `BioTrace_Abp_Test`、`BioTrace_Elsa_Test` 在 Postgres 可达时由测试 fixture 自动创建（亦见 [`docker/postgres/init/01-create-databases.sql`](docker/postgres/init/01-create-databases.sql)）。

VS Code / Cursor 任务：**test-solution**（单元测）、**test-integration**（集成测）。

演示 Host 的 `appsettings.json` **未**启用 Password Grant；仅 WAF 注入 `AuthServer:AllowPasswordGrantForIntegrationTests=true` 时生效。

在宿主应用中除 `AbpHttpApiModule` 外，还需引用 `ElsaAbpAspNetCoreModule`（见上文「宿主集成清单」）。

## Git Flow

本仓库采用 **Git Flow**：

| 分支 | 说明 |
|------|------|
| `main` | 生产就绪；仅通过 `release/*`、`hotfix/*` 合并 |
| `develop` | 日常集成；`feature/*` 合并目标 |
| `feature/*` | 新功能（从 `develop` 拉出） |
| `release/*` | 发版准备（合并到 `main` 与 `develop`） |
| `hotfix/*` | 生产紧急修复（从 `main` 拉出） |

日常开发请基于 `develop` 创建功能分支，详见 [CONTRIBUTING.md](CONTRIBUTING.md)。

```bash
git checkout develop
git pull origin develop
git checkout -b feature/my-feature
```

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

向 `main` / `develop` 的 PR，以及对 `main`、`develop`、`feature/*`、`release/*`、`hotfix/*` 的推送会触发 [GitHub Actions](.github/workflows/ci.yml)：

| Job | 内容 |
|-----|------|
| `build-and-test` | 编译 + **单元测**（`Category!=Integration`，不依赖 Postgres） |
| `integration-tests` | **Host 集成测**（GHA `postgres:15-alpine` service，T0–T5） |

PR 合并前两个 job 均须通过。

## NuGet 发布准备

仓库已提供统一打包元数据（见 `common.props`）与发布工作流 [`.github/workflows/nuget-publish.yml`](.github/workflows/nuget-publish.yml)，用于将模块各层按同版本发布到 NuGet.org。

消费方如何安装包、配置数据库与权限，见 **[消费方集成指南](docs/nuget-consumer-guide.md)**。

### 将会发布的包

| 项目 | 包名 |
|---|---|
| `BioTrace.Elsa.Abp.Domain.Shared` | `BioTrace.Elsa.Abp.Domain.Shared` |
| `BioTrace.Elsa.Abp.Domain` | `BioTrace.Elsa.Abp.Domain` |
| `BioTrace.Elsa.Abp.Application.Contracts` | `BioTrace.Elsa.Abp.Application.Contracts` |
| `BioTrace.Elsa.Abp.Application` | `BioTrace.Elsa.Abp.Application` |
| `BioTrace.Elsa.Abp.HttpApi` | `BioTrace.Elsa.Abp.HttpApi` |
| `BioTrace.Elsa.Abp.HttpApi.Client` | `BioTrace.Elsa.Abp.HttpApi.Client` |
| `BioTrace.Elsa.Abp.EntityFrameworkCore` | `BioTrace.Elsa.Abp.EntityFrameworkCore` |
| `BioTrace.Elsa.Abp.AspNetCore` | `BioTrace.Elsa.Abp.AspNetCore` |
| `BioTrace.Elsa.Abp.Studio.BlazorWasm` | `BioTrace.Elsa.Abp.Studio.BlazorWasm` |
| `BioTrace.Elsa.Abp.Studio.AspNetCore` | `BioTrace.Elsa.Abp.Studio.AspNetCore` |
| `BioTrace.Elsa.Abp.Installer` | `BioTrace.Elsa.Abp.Installer` |

### 发布前检查清单

1. 版本号对齐：更新 `common.props` 的 `<Version>`（或发布时由 workflow 输入/Tag 覆盖）。
2. 通过 CI：确保编译、单元测、集成测全部通过。
3. README 完整：确认模块集成方式、连接串、权限说明与版本策略无误。
4. 仓库 Secret：在 GitHub 仓库设置 `NUGET_API_KEY`（NuGet.org API Key）。
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

工作流会先产出 `artifacts/nuget` 并上传构建产物，再在 `NUGET_API_KEY` 存在时执行 `dotnet nuget push --skip-duplicate`。

## 许可证

本项目采用 [MIT License](LICENSE) 开源。
