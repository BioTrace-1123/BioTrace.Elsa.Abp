# BioTrace.Elsa.Abp 安装说明

本模块将 **Elsa 3.x** 工作流引擎集成到 **ABP** 应用：Elsa 持久化与 ABP 业务库分离，权限与 OpenIddict 声明桥接，可选多租户与 Elsa Studio。

## 安装方式

发布到 NuGet 后，在目标 ABP 解决方案中：

```bash
abp add-module BioTrace.Elsa.Abp
```

或在 **ABP Studio** 解决方案资源管理器中使用 **Install module**，选择 `BioTrace.Elsa.Abp`。

CLI / Studio 会下载 `BioTrace.Elsa.Abp.Installer`，按 `.abpmdl` 与各包 `.abppkg` 角色添加 NuGet 引用与 `[DependsOn(...)]`。

> 本模块**不包含** ABP Identity / OpenIddict / Permission 的 EF 实现；调用方需自行引用官方包。本模块的 `EntityFrameworkCore` 项目仅用于仓库内演示 Host，**不**发布 NuGet，故不在安装元数据中。

## 安装后必做配置

自动安装**不会**完成以下步骤，请参阅 [NuGet 消费者指南](../../docs/nuget-consumer-guide.md)：

0. **合并配置**（推荐）：
   ```bash
   ./scripts/merge-appsettings-elsa.sh --target src/YourApp.HttpApi.Host/appsettings.json
   ```
   Studio Client 另合并 [`docs/appsettings.elsa.studio.json`](../../docs/appsettings.elsa.studio.json) 到 `wwwroot/appsettings.json`。
1. **Elsa 持久化**：引用 `Elsa.Persistence.EFCore.{PostgreSql|SqlServer|Sqlite}`，并创建继承 `ElsaAbpAspNetCoreModule` 的模块，重写 `ConfigureElsaPersistence`。
2. **连接串**：配置 `ConnectionStrings:Default`（ABP）与 `ConnectionStrings:Elsa`（Elsa，必填且与 Default 分离）。
3. **Host 管道**：`UseMultiTenancy()`（若启用）→ `UseElsaAbpMultiTenancy()` → `UseElsaWorkflows()`。
4. **权限**：为角色授予 `Abp.Elsa.*` 权限（见消费者指南）。
5. **Elsa Studio（可选）**：
   - 运行脚手架生成 WASM Client（**会自动**向 Host 添加 `BioTrace.Elsa.Abp.Studio.AspNetCore` NuGet 引用与 Client `ProjectReference`）：
     ```bash
     ./scripts/scaffold-elsa-studio-client.sh \
       --name YourApp.Studio.Client \
       --output src/YourApp.Studio.Client \
       --host-project src/YourApp.HttpApi.Host/YourApp.HttpApi.Host.csproj \
       --solution YourApp.sln \
       --register-host-module
     ```
   - `BioTrace.Elsa.Abp.Studio.Client` **不**发布 NuGet；脚手架在调用方解决方案内创建仅含 `Program.cs`、`appsettings.json`、`.csproj` 的项目（`index.html` 由 `Studio.BlazorWasm` 提供）。
   - Host 调用 `AddBioTraceElsaAbpStudioHost()` / `UseBioTraceElsaAbpStudioHost()`；Client 调用 `AddBioTraceElsaAbpStudio()`。
6. **DbMigrator（生产）**：参考演示 [`host/BioTrace.Elsa.Abp.DbMigrator`](../../host/BioTrace.Elsa.Abp.DbMigrator)；`Elsa:RunMigrations=false` 时在迁移程序中调用 `await MigrateElsaDatabasesAsync(force: true)`。

## 文档

- 集成清单与故障排查：[docs/nuget-consumer-guide.md](../../docs/nuget-consumer-guide.md)
- 仓库 README：[README.md](../../README.md)
