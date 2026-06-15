# Changelog

本文件记录 [BioTrace.Elsa.Abp](https://github.com/BioTrace-1123/BioTrace.Elsa.Abp) NuGet 包的重要变更。

格式基于 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/)。

## [Unreleased]

### Added

- `BioTrace.Elsa.Abp.AspNetCore`：`ElsaAbpElsaDatabaseMigrationHostedService` 与 `MigrateElsaDatabasesAsync` 扩展，由 `Elsa:RunMigrations` 控制 Elsa 工作流库迁移。
- `ElsaAbpOptions.MigrateOnlyInDevelopment`（默认 `true`）：生产环境默认不在 Host 启动时迁移 Elsa 库。
- `BioTrace.Elsa.Abp.Application`：`ElsaAbpPermissionDataSeedContributor` 可配置权限种子基类。
- `BioTrace.Elsa.Abp.Studio.BlazorWasm`：可选通过 ABP `GET /api/multi-tenancy/tenants` 动态加载租户列表（`ElsaStudio:Tenancy:UseAbpTenantApi`）。
- `BioTrace.Elsa.Abp.IntegrationTesting`：可复用集成测试辅助包。
- 脚手架 `scaffold-elsa-studio-client.sh` 自动向 Host 添加 `BioTrace.Elsa.Abp.Studio.AspNetCore` 引用；`--register-host-module` 可注入 Host 中间件代码。
- `docs/appsettings.elsa.json` 与 `scripts/merge-appsettings-elsa.sh` 配置模板。

### Changed

- Elsa EF 持久化应设置 `ef.RunMigrations = false`，统一由 `ElsaAbpElsaDatabaseMigrationHostedService` 或 `MigrateElsaDatabasesAsync` 迁移。
- `BioTrace.Elsa.Abp.Studio.AspNetCore.abppkg` 增加 `lib.host` 角色，Installer 可挂载至 Host 项目。
- 演示 Host 移除 `UseBioTraceElsaAbpStudioFallback()` 调用。

### Deprecated

- `UseBioTraceElsaAbpStudioFallback()`：SPA 路由重写已合并至 `UseBioTraceElsaAbpStudioHost()`，该方法为 no-op，将在 **2.0** 移除。

## [1.0.0-preview.4] - 历史说明

### Changed

- `UseBioTraceElsaAbpStudioFallback()` 自 preview.2 起逐步变为 no-op；实际 `/studio` SPA 路由由 `UseBioTraceElsaAbpStudioHost()` 中间件处理。
