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

## 合并前检查

```bash
dotnet build BioTrace.Elsa.Abp.slnx
dotnet test BioTrace.Elsa.Abp.slnx --filter "Category!=Integration"

# 可选（需 PostgreSQL；Dev Container 内可直接运行）
./scripts/test-integration.sh
./scripts/test-e2e.sh
```

涉及 Elsa Studio、OIDC 登录流或多租户 UI 的改动，建议本地跑通 `./scripts/test-e2e.sh` 或相关 spec。说明见 [README — 浏览器 E2E](README.md#浏览器-e2eplaywright)。

CI 会在 PR 与相关分支推送时自动运行（见 `.github/workflows/ci.yml`）：`build-and-test`（单元测）、`integration-tests`（Host 集成测）与 `e2e-tests`（Playwright 浏览器 E2E）均须通过。

## GitHub 仓库建议设置

1. **默认分支**：`develop`（日常 PR 目标）
2. **分支保护**（`main`、`develop`）：
   - Require pull request before merging
   - Require status checks to pass（CI `build-and-test`）
   - 禁止直接 force push
3. **删除合并后的分支**：在 PR 设置中启用 “Automatically delete head branches”
