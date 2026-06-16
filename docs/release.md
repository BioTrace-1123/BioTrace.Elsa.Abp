# NuGet 发布指南

本文面向**维护者**，说明如何将本模块发布到 NuGet.org。调用方安装与配置见 [调用方集成指南](nuget-consumer-guide.md)。

仓库提供统一打包元数据（[`common.props`](../common.props)）与发布工作流 [`.github/workflows/nuget-publish.yml`](../.github/workflows/nuget-publish.yml)。**仅发布 Elsa / Studio 集成相关库**；`host/` 与 `test/`（除 `IntegrationTesting`）不打包。

## 发布前检查清单

1. **版本号**：更新 `common.props` 的 `<Version>`（或发布时由 workflow 输入/Tag 覆盖）。
2. **CI**：确保编译、单元测、集成测、E2E 全部通过（见 [CONTRIBUTING.md](../CONTRIBUTING.md)）。
3. **README**：确认模块集成方式、连接串、权限说明与版本策略无误。
4. **NuGet Trusted Publishing**：在 [nuget.org Trusted Publishing](https://www.nuget.org/account/trustedpublishing) 配置策略（Repository Owner: `BioTrace-1123`，Repository: `BioTrace.Elsa.Abp`，Workflow: `nuget-publish.yml`，Package owner 与下方 Secret 一致）；在 GitHub 仓库 Secrets 设置 `NUGET_USER`（NuGet.org **用户名**，非邮箱）。无需长期 `NUGET_API_KEY`。
5. **许可证**：根目录 `LICENSE`（MIT），NuGet 元数据见 `common.props` 的 `PackageLicenseExpression`。
6. **传递依赖**：运行 `./scripts/verify-nuget-dependencies.sh`，或依赖 CI `nuget-pack-verify` job，确保所有 `BioTrace.Elsa.Abp.*` 传递依赖均已打包。

## 本地打包验证

```bash
dotnet restore BioTrace.Elsa.Abp.slnx
dotnet build BioTrace.Elsa.Abp.slnx -c Release

dotnet pack src/BioTrace.Elsa.Abp.AspNetCore/BioTrace.Elsa.Abp.AspNetCore.csproj \
  -c Release --no-build -o ./artifacts/nuget
```

需要一次性打全部包时，可按 `nuget-publish.yml` 中 `Pack module projects` 的列表逐个执行 `dotnet pack`。

## GitHub Actions 发布方式

支持两种触发方式：

- **手工触发**：`Actions -> NuGet Publish -> Run workflow`，输入版本号（如 `1.2.3`）。
- **Tag 触发**：推送 `v*.*.*`（如 `v1.2.3`）后自动发布。

工作流会先产出 `artifacts/nuget` 并上传构建产物，再通过 OIDC（`NuGet/login@v1`）换取短期密钥并执行 `dotnet nuget push --skip-duplicate`。

## 将会发布的包

| 包名 | 说明 |
|------|------|
| `BioTrace.Elsa.Abp.AspNetCore` | **集成入口**（Elsa 注册、权限桥接、多租户） |
| `BioTrace.Elsa.Abp.HttpApi` | `current-user` 等 ABP API |
| `BioTrace.Elsa.Abp.Studio.BlazorWasm` | Studio UI 与 ABP 租户/权限组件 |
| `BioTrace.Elsa.Abp.Studio.AspNetCore` | 调用方内嵌 `/studio` 托管扩展（不含 WASM 壳） |
| `BioTrace.Elsa.Abp.Application` | 示例 Activity 等（`AspNetCore` 传递） |
| `BioTrace.Elsa.Abp.Application.Contracts` | 权限常量、DTO（传递或单独引用） |
| `BioTrace.Elsa.Abp.Domain` / `Domain.Shared` | 选项与常量（传递） |
| `BioTrace.Elsa.Abp.HttpApi.Client` | 可选：动态 API 代理 |
| `BioTrace.Elsa.Abp.Installer` | 可选：ABP CLI / Studio 安装元数据（`abp add-module BioTrace.Elsa.Abp`） |
| `BioTrace.Elsa.Abp.IntegrationTesting` | 可选：集成测试 WebApplicationFactory 与 OpenIddict 辅助 |
