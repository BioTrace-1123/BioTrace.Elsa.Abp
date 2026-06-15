# Nexus / LeptonXLite UI 主题集成

演示 Host 使用 **ABP Basic Theme**；生产调用方常用 **Nexus**、**LeptonXLite** 或 **LeptonX**。本页说明与 Elsa Studio 集成时的差异与配置要点。

## 登录页与账户管理

| 主题 | 典型登录 URL | 说明 |
|------|-------------|------|
| Basic Theme（演示） | `/Account/Login` | 演示 Host 默认 |
| Nexus / LeptonXLite | 由主题模块决定，常见为 `/Account/Login` 或主题文档中的路径 | 布局与样式不同，OIDC 流程相同 |
| 账户管理 | Host 侧 ABP Account 模块 | Studio **不**替代 Host 登录页 |

租户用户登录时须在 Host 登录页选择或填写租户（查询参数 `__tenant=tenant-a` 或主题提供的租户选择控件）。

## Elsa Studio 回调 URI（与主题无关）

无论 Host 使用何种 MVC 主题，OpenIddict 客户端 `ElsaStudio` 的 RedirectUri 始终为：

```
{Authority}{ElsaStudio:PathBase}/authentication/login-callback
```

默认 PathBase 为 `/studio`，即 `https://api.example.com/studio/authentication/login-callback`。

修改 `ElsaStudio:PathBase` 后须：

1. 更新 Host `appsettings.json` 中 `OpenIddict:Applications:ElsaStudio:RedirectUris`
2. 重新运行 DbMigrator/数据种子（`ElsaAbpOpenIddictDataSeedContributor` 会合并更新 URI）
3. 同步 Studio Client `wwwroot/appsettings.json` 中的 `ElsaStudio:Authentication:Authority`

## Studio Client 配置

参考 [`appsettings.elsa.studio.json`](appsettings.elsa.studio.json) 合并到 WASM Client：

- `ElsaStudio:Authentication:Authority` → Host 根地址（与 OpenIddict Authority 一致）
- `ElsaStudio:Backend:Url` → `{Authority}/elsa/api`
- `ElsaStudio:Tenancy:UseAbpTenantApi` → `true` 时从 `GET /api/multi-tenancy/tenants` 加载租户（Host 用户切换场景）

Host 须引用 `Volo.Abp.TenantManagement.HttpApi` 并 `[DependsOn(typeof(AbpTenantManagementHttpApiModule))]`。

## 前端资源（非 Basic Theme）

演示 Host 的 `package.json` 仅包含 Basic Theme 的 `@abp/aspnetcore.mvc.ui.theme.basic`。使用 Nexus/LeptonXLite 时：

1. 按 ABP 主题文档安装对应 npm 包
2. 在 Host 项目运行 `abp install-libs` 或 `npm install`
3. 在 Host 模块中替换 `AbpAspNetCoreMvcUiBasicThemeModule` 为主题模块（如 `AbpAspNetCoreMvcUiLeptonXLiteThemeModule`）

Elsa 中间件与 Studio 托管 **不**依赖 MVC 主题包。

## 多租户 UX 对照

| 用户类型 | Host 侧 | Studio 侧 |
|----------|---------|------------|
| Host 管理员 | 主题登录页 + 可选租户管理 UI | MudSelect 切换租户（`UseAbpTenantApi`） |
| 租户用户 | 登录时须带 `__tenant` | 租户锁定，无下拉 |

## 相关文档

- [NuGet 消费者指南](nuget-consumer-guide.md)
- [README 安全集成](../README.md#安全集成abp-openiddict--elsa-370)
