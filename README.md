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

## 本地运行 Host（PostgreSQL）

```bash
docker compose up -d
dotnet run --project host/BioTrace.Elsa.Abp.HttpApi.Host
```

- API / Swagger：`https://localhost:44388`
- Elsa Workflows API：由 `UseWorkflowsApi` 暴露（路径以 Elsa 默认为准）
- `docker/postgres/init` 会创建 `BioTrace_Abp` 与 `BioTrace_Elsa` 两个库

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
