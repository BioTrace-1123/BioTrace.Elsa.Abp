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
