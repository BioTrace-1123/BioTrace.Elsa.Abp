# Elsa 小版本升级检查清单

本模块**不提供** Elsa 3.x 大版本或官方 EF 迁移的逐步指南；表结构变更请参照 [Elsa 官方文档](https://elsaworkflows.io/)。以下为调用方在升级 `Elsa*` NuGet 时的**自检清单**。

## 升级前

- [ ] 在 `Directory.Build.props`（或各项目）将 **所有** `Elsa*` / `Elsa.Persistence.EFCore.*` 包设为**同一版本**（与 `BioTrace.Elsa.Abp` 发布说明中的 `ElsaPackageVersion` 对齐或按 Release Notes 升级）
- [ ] 阅读 Elsa Release Notes 中的 Breaking Changes 与 EF 迁移说明
- [ ] 在开发/预发环境备份 Elsa 工作流库（`ConnectionStrings:Elsa`）

## 升级中

- [ ] 确认 `ConfigureElsaPersistence` 中 `ef.RunMigrations = false`（迁移由 `ElsaAbpElsaDatabaseMigrator` 或 DbMigrator `MigrateElsaDatabasesAsync(force: true)` 执行）
- [ ] 运行调用方 DbMigrator（或 CI）迁移 ABP 库 + `await MigrateElsaDatabasesAsync(force: true)`
- [ ] 若启用多租户，确认 `Elsa:EnableMultiTenancy` 与 ABP `MultiTenancyConsts.IsEnabled` 一致

## 升级后验证

- [ ] `dotnet test` — 模块集成测（若引用 `BioTrace.Elsa.Abp.IntegrationTesting`）
- [ ] Host 启动无 `ElsaPersistenceNotConfigured` / 连接串错误
- [ ] `GET /api/abp/elsa/current-user` 与 `/elsa/api/...` 鉴权正常
- [ ] Studio 登录与租户切换（若启用）
- [ ] 抽查既有工作流定义发布与实例执行

## 责任边界

| 项 | 负责方 |
|----|--------|
| Elsa EF 表结构 / 官方迁移 | Elsa 官方 + 调用方 DbMigrator |
| `BioTrace.Elsa.Abp` API 与 ABP 桥接 | 本模块 NuGet + CHANGELOG |
| ABP Identity / OpenIddict / Permission | 调用方 ABP 版本与迁移 |
