# Changelog

本文件记录 [BioTrace.Elsa.Abp](https://github.com/BioTrace-1123/BioTrace.Elsa.Abp) NuGet 包的重要变更。

格式基于 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/)。

## [Unreleased]

## [1.0.0] - 2026-06-15

### Added

- `IElsaDatabaseMigrator` 与 `MigrateElsaDatabasesAsync(force: true)`：生产 `Elsa:RunMigrations=false` 时 DbMigrator/CI 可强制迁移 Elsa 库。
- `ElsaAbpOptions.DeferTenantActivationUntilReady`：ABP 租户库未就绪时延迟 Elsa 租户激活，避免冷启动 FATAL。
- `ElsaAbpDeferringTenantsProvider`：配合上述选项的 `ITenantsProvider` 装饰器。
- `ElsaAbpOpenIddictDataSeedContributor` 纳入 `BioTrace.Elsa.Abp.Application`（配置驱动 OpenIddict 客户端，支持 RedirectUri 合并更新）。
- `IElsaAbpPermissionSeeder`（`Application.Contracts`）与既有 `ElsaAbpPermissionDataSeedContributor` 基类。
- 演示 `host/BioTrace.Elsa.Abp.DbMigrator`：ABP 库 + `MigrateElsaDatabasesAsync(force: true)` 样板。
- `docs/appsettings.elsa.studio.json`、`docs/ui-theme-nexus.md`、`docs/elsa-upgrade-checklist.md`、`docs/samples/HttpApi.Host.Tests/`。
- `BioTrace.Elsa.Abp.AspNetCore`：`ElsaAbpElsaDatabaseMigrationHostedService` 与 `MigrateElsaDatabasesAsync` 扩展。
- `ElsaAbpOptions.MigrateOnlyInDevelopment`（默认 `true`）。
- `BioTrace.Elsa.Abp.Studio.BlazorWasm`：可选通过 ABP `GET /api/multi-tenancy/tenants` 动态加载租户列表。
- `BioTrace.Elsa.Abp.IntegrationTesting`：可复用集成测试辅助包。
- 脚手架 `scaffold-elsa-studio-client.sh`；`docs/appsettings.elsa.json` 与 `scripts/merge-appsettings-elsa.sh`。

### Changed

- `ElsaAbpElsaDatabaseMigrator` 在 `RunMigrations=false` 时仍注册 DI，供 DbMigrator 显式调用。
- Elsa EF 持久化应设置 `ef.RunMigrations = false`，统一由模块迁移器迁移。
- `BioTrace.Elsa.Abp.Studio.AspNetCore.abppkg` 增加 `lib.host` 角色。
- 演示 Host 移除 `UseBioTraceElsaAbpStudioFallback()` 调用。

### Deprecated

- `UseBioTraceElsaAbpStudioFallback()`：no-op，将在 **2.0** 移除。

## [1.0.0-preview.4] - 历史说明

### Changed

- `UseBioTraceElsaAbpStudioFallback()` 自 preview.2 起逐步变为 no-op。
