# 浏览器 E2E（Playwright）

本目录通过 **真实 OIDC Authorization Code + PKCE** 登录 Elsa Studio，验证 UI、多租户切换与工作流生命周期（创建/发布/执行/取消）。**不在** `BioTrace.Elsa.Abp.slnx` 内，使用独立 npm 包与 Playwright。

## 前置条件

| 项 | 说明 |
|----|------|
| PostgreSQL | 宿主机：`docker compose up -d`；Dev Container：Compose `postgres` 服务已就绪 |
| Node.js | 与 Dev Container / CI 一致（E2E 目录 `npm ci`） |
| Playwright Chromium | `./scripts/test-e2e.sh` 会自动 `playwright install chromium`；Dev Container 在 `postCreateCommand` 中已预装 |
| HTTPS 证书 | 宿主机首次运行需 `dotnet dev-certs https --trust`（CI 与 Dev Container 已处理） |

E2E 使用独立库 `BioTrace_Abp_E2E`、`BioTrace_Elsa_E2E`（见 `docker/postgres/init`）。`playwright.config.ts` 的 `webServer` 会在每次运行前重建上述库并自动启动演示项目（Development Migrate + Seed），**无需**手动 `dotnet run` Host。

## 一键运行

```bash
# 宿主机（先 docker compose up -d）
./scripts/test-e2e.sh

# Dev Container 内（Postgres 主机为 postgres）
E2E_POSTGRES_HOST=postgres ./scripts/test-e2e.sh
```

环境变量（可选）：

| 变量 | 默认 | 说明 |
|------|------|------|
| `E2E_POSTGRES_HOST` | `localhost`（或继承 `INTEGRATION_TEST_POSTGRES_HOST`） | PostgreSQL 主机 |
| `E2E_BASE_URL` | `https://localhost:44388` | 演示项目基址 |

VS Code 任务：**test-e2e**（等价于 `./scripts/test-e2e.sh`）。

## 本地调试

```bash
cd test/BioTrace.Elsa.Abp.E2E
npm ci
npx playwright install chromium

# 仍由 webServer 自动起 Host；仅跑单个 spec：
npx playwright test tests/studio-smoke.spec.ts

# 可视化调试
npm run test:e2e:ui
npm run test:e2e:headed

# 查看 HTML 报告
npm run show-report
```

## 用例概览

| Spec | 场景 |
|------|------|
| `auth.setup.ts` | 为各角色执行 OIDC 登录并缓存 `storageState` |
| `studio-smoke.spec.ts` | `admin` 加载 Studio、打开工作流定义列表 |
| `studio-auth-routing.spec.ts` | OIDC 登录回调与 Studio 路由 |
| `studio-tenancy.spec.ts` | 租户 A/B 列表隔离；Host `admin` 切换租户后可见租户 A 演示流 |
| `studio-permissions.spec.ts` | `tenant-a-designer` 只读不可创建；`tenant-a-admin` 可创建 |
| `studio-workflow-lifecycle.spec.ts` | 创建 → 发布 → 执行 → 实例完成 |
| `studio-instance-cancel.spec.ts` | 运行中 Delay 工作流实例取消 |

## 演示账户

与演示 Host 种子一致（密码均为 `1q2w3E*`）：

| 用户 | 租户 | 用途 |
|------|------|------|
| `admin` | Host（根租户） | Studio 冒烟、根租户上下文代管 |
| `tenant-a-admin` | `tenant-a` | 工作流 CRUD/执行/取消 |
| `tenant-a-designer` | `tenant-a` | 只读权限 |
| `tenant-b-admin` | `tenant-b` | 租户隔离 |

> 种子还包含 `tenant-b-designer`（租户只读），当前 E2E 未为其单独建 Playwright project。

## CI

`e2e-tests` job（Postgres + `dotnet dev-certs https` + Playwright Chromium，`ignoreHTTPSErrors` 无需系统信任）；失败时上传 `playwright-report` 构件。

演示项目的 `appsettings.json` **未**启用 Password Grant；仅 WAF 注入 `AuthServer:AllowPasswordGrantForIntegrationTests=true` 时生效（集成测专用，E2E 走真实 OIDC）。
