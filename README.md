# BioTrace.Elsa.Abp

基于 [ABP Framework](https://abp.io/) 的可复用 **Application Module**，用于在宿主应用中集成 BioTrace / Elsa 相关能力。

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
  BioTrace.Elsa.Abp.EntityFrameworkCore  # ABP 业务 DbContext（连接名 Abp）
  BioTrace.Elsa.Abp.AspNetCore           # Elsa 服务注册（连接名 Elsa）
  BioTrace.Elsa.Abp.HttpApi
  BioTrace.Elsa.Abp.HttpApi.Client
  BioTrace.Elsa.Abp.Installer
host/
  BioTrace.Elsa.Abp.HttpApi.Host         # 本地验证宿主
test/
  BioTrace.Elsa.Abp.TestBase
  BioTrace.Elsa.Abp.*.Tests
```

## 宿主集成清单

引用本模块的 ABP 应用**必须**单独配置 Elsa 数据库；仅引用 NuGet/项目**不会**自动创建 Elsa 表。

1. 在宿主启动模块上添加依赖：
   ```csharp
   [DependsOn(typeof(ElsaAbpAspNetCoreModule), typeof(AbpHttpApiModule))]
   ```
2. 在 `appsettings.json` 中配置**独立**连接串（名称默认为 `Elsa`，与 `Abp` 业务库分离）：
   ```json
   {
     "ConnectionStrings": {
       "Abp": "Host=...;Database=your_abp_db;...",
       "Elsa": "Host=...;Database=your_elsa_db;..."
     },
     "Elsa": {
       "RunMigrations": true,
       "EnableWorkflowsApi": true,
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
    "CorsOrigins": ["https://localhost:44388", "https://localhost:5003"]
  },
  "AuthServer": {
    "Authority": "https://localhost:44388",
    "SwaggerClientId": "BioTrace_Elsa_Abp_Swagger"
  },
  "OpenIddict": {
    "Applications": {
      "ElsaStudio": {
        "ClientId": "ElsaStudio",
        "RootUrl": "https://localhost:5003",
        "RedirectUris": [
          "https://localhost:5003/authentication/login-callback"
        ]
      }
    }
  },
  "Elsa": {
    "EnablePermissionClaimsBridge": true,
    "DisableElsaEndpointSecurity": false
  }
}
```

- CORS 必须**显式 Origin** + `AllowCredentials()`，禁止 `AllowAnyOrigin()` 与 OIDC 混用。
- Swagger：OAuth2 Authorization Code，ClientId `BioTrace_Elsa_Abp_Swagger`，Scope `BioTrace_Elsa_Abp`。
- Elsa Studio：`Backend.Url` 指向 Host（如 `https://localhost:44388`），Code Flow 客户端 `ElsaStudio`。
- 生产环境勿设置 `Elsa:DisableElsaEndpointSecurity=true`。

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

### 宿主额外依赖（演示）

除 `ElsaAbpAspNetCoreModule` 外，演示 Host 引用 Identity / OpenIddict / PermissionManagement EF 模块，并将 `AbpIdentity`、`AbpOpenIddict`、`AbpPermissionManagement` 连接串指向同一 `ConnectionStrings:Abp` 库（与 Elsa 库仍分离）。

## 本地运行 Host（PostgreSQL）

```bash
docker compose up -d
dotnet run --project host/BioTrace.Elsa.Abp.HttpApi.Host
```

- API / Swagger：`https://localhost:44388`
- Elsa Workflows API：由 `UseWorkflowsApi` 暴露（路径以 Elsa 默认为准）
- `docker/postgres/init` 会创建 `BioTrace_Abp` 与 `BioTrace_Elsa` 两个库

## 使用 Dev Container（推荐）

克隆仓库后，可用 VS Code / Cursor 的 [Dev Containers](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers) 一键进入容器化开发环境（含 PostgreSQL 与 .NET 10 SDK）。

**前置**：安装 [Docker Desktop](https://www.docker.com/products/docker-desktop/)（启用 **WSL 2** 引擎），以及 Dev Containers 扩展。仓库在 WSL 内时，须在 Docker Desktop → **Settings → Resources → WSL integration** 中为 **Ubuntu** 打开集成，并在 WSL 终端能执行 `docker version`。

1. 打开仓库根目录（推荐：先用 **WSL: Connect to WSL** 打开 `/root/source/repos/BioTrace.Elsa.Abp`，再 **Reopen in Container**；避免仅从 Windows 侧打开 `\\wsl.localhost\...` 却未启用 WSL 集成）。
2. 命令面板执行 **Dev Containers: Reopen in Container**。
3. 等待镜像构建与 `postCreate`（`dotnet dev-certs https --trust`、`dotnet restore`）。
4. 按 **F5**，选择 **Launch HttpApi.Host (HTTPS)**。
5. 浏览器访问 `https://localhost:44388`（Swagger）。

容器内通过环境变量将数据库主机设为 Compose 服务名 `postgres`（`ConnectionStrings__Abp` / `ConnectionStrings__Elsa`），不影响在宿主机上直接使用 `appsettings.json` 里的 `localhost` 连接串。

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
dotnet test BioTrace.Elsa.Abp.slnx
```

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

向 `main` / `develop` 的 PR，以及对 `main`、`develop`、`feature/*`、`release/*`、`hotfix/*` 的推送会触发 [GitHub Actions](.github/workflows/ci.yml)。

## 许可证

尚未指定许可证。若计划开源，请在仓库根目录添加 `LICENSE` 并更新本节。
