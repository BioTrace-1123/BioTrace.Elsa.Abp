# 贡献指南

本仓库采用 [Git Flow](https://nvie.com/posts/a-successful-git-branching-model/) 分支模型。

## 长期分支

| 分支 | 用途 |
|------|------|
| `main` | 生产就绪代码；仅接受来自 `release/*`、`hotfix/*` 的合并 |
| `develop` | 日常集成分支；功能开发合并到此 |

## 短期分支

| 前缀 | 从何处拉出 | 合并到 | 示例 |
|------|------------|--------|------|
| `feature/` | `develop` | `develop` | `feature/elsa-workflow-hooks` |
| `release/` | `develop` | `main` **且** `develop` | `release/1.1.0` |
| `hotfix/` | `main` | `main` **且** `develop` | `hotfix/fix-permission-seed` |

命名使用小写与连字符：`feature/add-sample-api`，避免空格与下划线。

## 日常开发（功能）

```bash
git checkout develop
git pull origin develop
git checkout -b feature/my-feature

# 开发、提交
git push -u origin feature/my-feature
```

在 GitHub 上向 **`develop`** 发起 Pull Request（不要直接推送到 `develop`）。

## 发布版本

```bash
git checkout develop
git pull origin develop
git checkout -b release/1.2.0

# 仅做版本号、CHANGELOG、最后一轮修复
git push -u origin release/1.2.0
```

1. 向 **`main`** 提 PR，合并后打 tag：`git tag -a v1.2.0 -m "Release 1.2.0"`
2. 将同一 `release/*` 合并回 **`develop`**（或提 PR 到 `develop`），保持两分支一致

## 紧急修复（热修复）

```bash
git checkout main
git pull origin main
git checkout -b hotfix/critical-fix

# 修复、测试
git push -u origin hotfix/critical-fix
```

1. PR 合并到 **`main`**，打 patch tag（如 `v1.2.1`）
2. 再合并到 **`develop`**，避免回归

## 提交信息

建议使用 [Conventional Commits](https://www.conventionalcommits.org/)：

```
feat: add Elsa activity registration
fix: resolve AbpModule cyclic dependency in EF layer
docs: update Git Flow section in README
chore: bump Volo.Abp to 10.4.1
```

## 开发环境

**要求**：[.NET SDK 10](https://dotnet.microsoft.com/download)（见仓库根目录 `global.json`）

### Dev Container（可选）

仓库提供 [Dev Containers](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers) 配置（`.devcontainer/`），内含 .NET 10 SDK、Node.js、PostgreSQL 与 Playwright 依赖。

1. 克隆仓库并在 VS Code 中打开根目录。
2. 命令面板执行 **Dev Containers: Reopen in Container**。
3. 等待 `postCreateCommand` 完成（`dotnet restore`、HTTPS 开发证书、演示项目前端依赖等）。
4. **F5** → **Launch HttpApi.Host (HTTPS)**，在浏览器打开 `/studio`、`/swagger`、`/swagger/elsa`。

容器内 PostgreSQL 主机为 Compose 服务名 `postgres`（见 `devcontainer.json` 中的 `ConnectionStrings__*` 与 `INTEGRATION_TEST_POSTGRES_HOST`），无需在容器内单独执行 `docker compose up -d`。

| 现象 | 处理 |
|------|------|
| 端口 `5432` 冲突 | 停止本地 PostgreSQL，或修改根目录 `docker-compose.yml` 端口映射 |
| HTTPS 证书不受信任 | 容器内执行 `dotnet dev-certs https --trust` |
| 数据库未就绪 | 等待 Compose 中 `postgres` 健康检查通过后再启动演示项目 |

不使用 Dev Container 时，在宿主机安装 .NET 10 并运行 `docker compose up -d`，参见 [README — 演示项目快速开始](README.md#演示项目快速开始)。

## 合并前检查

```bash
dotnet restore BioTrace.Elsa.Abp.slnx
dotnet build BioTrace.Elsa.Abp.slnx
dotnet test BioTrace.Elsa.Abp.slnx --filter "Category!=Integration"

# 可选（需 PostgreSQL；Dev Container 内可直接运行）
./scripts/test-integration.sh
./scripts/test-e2e.sh
# Dev Container 内可显式指定 Postgres 主机
E2E_POSTGRES_HOST=postgres ./scripts/test-e2e.sh
```

### 测试分层

| 层级 | 项目 | 依赖 | 命令 |
|------|------|------|------|
| 单元测 | `BioTrace.Elsa.Abp.*.Tests`（含 Mapper/Provider/Contributor） | 无 Postgres | `dotnet test --filter "Category!=Integration"` |
| 演示项目集成测 | [`HttpApi.Host.Tests`](test/BioTrace.Elsa.Abp.HttpApi.Host.Tests/) | Postgres | [`./scripts/test-integration.sh`](scripts/test-integration.sh) |
| 浏览器 E2E | [`BioTrace.Elsa.Abp.E2E`](test/BioTrace.Elsa.Abp.E2E/) | Postgres + Playwright | [`./scripts/test-e2e.sh`](scripts/test-e2e.sh) |

集成测 T0–T11 用例矩阵见 [集成测试样板](docs/samples/HttpApi.Host.Tests/README.md)。涉及 Elsa Studio、OIDC 登录流或多租户 UI 的改动，建议本地跑通 `./scripts/test-e2e.sh`；E2E 详情见 [test/BioTrace.Elsa.Abp.E2E/README.md](test/BioTrace.Elsa.Abp.E2E/README.md)。

## CI

向 `main` / `develop` 的 **PR**（含 `feature/*`、`release/*`、`hotfix/*` 等源分支），以及合并后对 `main`、`develop` 的 **push** 会触发 [GitHub Actions](.github/workflows/ci.yml)（同一 PR 更新只跑一套，不重复触发 push + pull_request）：

| Job | 内容 |
|-----|------|
| `build-and-test` | 编译 + **单元测**（`Category!=Integration`，不依赖 Postgres） |
| `integration-tests` | **演示项目集成测**（GHA `postgres:15-alpine` service，T0–T11） |
| `e2e-tests` | **Playwright 浏览器 E2E**（Postgres + Chromium，OIDC + Studio 全链路） |

PR 合并前三个 job 均须通过。

## NuGet 发布

维护者发布流程、Trusted Publishing 与本地 `dotnet pack` 见 [docs/release.md](docs/release.md)。

## GitHub 仓库建议设置

1. **默认分支**：`develop`（日常 PR 目标）
2. **分支保护**（`main`、`develop`）：
   - Require pull request before merging
   - Require status checks to pass（CI `build-and-test`）
   - 禁止直接 force push
3. **删除合并后的分支**：在 PR 设置中启用 “Automatically delete head branches”
