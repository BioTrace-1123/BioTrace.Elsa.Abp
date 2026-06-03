# BioTrace.Elsa.Abp

基于 [ABP Framework](https://abp.io/) 的可复用 **Application Module**，用于在宿主应用中集成 BioTrace / Elsa 相关能力。

## 技术栈

- .NET 10
- ABP 10.4（DDD 模块模板）
- Entity Framework Core

## 解决方案结构

```
src/
  BioTrace.Elsa.Abp.Domain.Shared
  BioTrace.Elsa.Abp.Domain
  BioTrace.Elsa.Abp.Application.Contracts
  BioTrace.Elsa.Abp.Application
  BioTrace.Elsa.Abp.EntityFrameworkCore
  BioTrace.Elsa.Abp.HttpApi
  BioTrace.Elsa.Abp.HttpApi.Client
  BioTrace.Elsa.Abp.Installer
test/
  BioTrace.Elsa.Abp.TestBase
  BioTrace.Elsa.Abp.*.Tests
```

## 本地开发

**要求**： [.NET SDK 10](https://dotnet.microsoft.com/download)（见仓库根目录 `global.json`）

```bash
dotnet restore BioTrace.Elsa.Abp.slnx
dotnet build BioTrace.Elsa.Abp.slnx
dotnet test BioTrace.Elsa.Abp.slnx
```

在宿主应用中引用本模块时，在启动模块上添加 `[DependsOn(typeof(AbpHttpApiModule))]`（或按需引用各层 `*Module`）。

## 在 GitHub 上发布

1. 在 GitHub 创建空仓库（不要勾选 “Add a README”，避免与本地历史冲突）。
2. 关联远程并推送：

```bash
git remote add origin https://github.com/<org>/<repo>.git
git push -u origin main
```

## CI

推送至 `main` / `develop` 或针对这些分支的 Pull Request 会触发 [GitHub Actions](.github/workflows/ci.yml)：还原、Release 构建与测试。

## 许可证

尚未指定许可证。若计划开源，请在仓库根目录添加 `LICENSE` 并更新本节。
