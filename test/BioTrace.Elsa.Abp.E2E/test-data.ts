/** Mirrors host/BioTrace.Elsa.Abp.HttpApi.Host/Data/ElsaAbpMultiTenancySeedData.cs */
export const TestData = {
  password: '1q2w3E*',
  baseUrl: process.env.E2E_BASE_URL ?? 'https://localhost:44388',
  studioPath: '/studio',
  oidcClientId: 'ElsaStudio',
  oidcAuthority: process.env.E2E_BASE_URL ?? 'https://localhost:44388',
  legacyAuthenticationLoginPath: '/authentication/login',

  users: {
    hostAdmin: 'admin',
    tenantAAdmin: 'tenant-a-admin',
    tenantBAdmin: 'tenant-b-admin',
    tenantADesigner: 'tenant-a-designer',
  },

  tenants: {
    tenantA: 'tenant-a',
    tenantB: 'tenant-b',
    tenantADisplayName: 'Tenant A',
    tenantBDisplayName: 'Tenant B',
    tenantCjk: '宝通',
    tenantCjkDisplayName: '宝通',
  },

  workflows: {
    demoTenantA: 'DemoTenantAWorkflow',
    demoTenantB: 'DemoTenantBWorkflow',
    demoCjkTenant: 'DemoCjkTenantWorkflow',
    e2eDelay: 'E2eDelayWorkflow',
  },
} as const;

export type AuthRole = 'admin' | 'tenant-a-admin' | 'tenant-a-designer' | 'tenant-b-admin';

export function authStoragePath(role: AuthRole): string {
  return `.auth/${role}.json`;
}

export function sessionStoragePath(role: AuthRole): string {
  return `.auth/${role}.session.json`;
}

export function localStoragePath(role: AuthRole): string {
  return `.auth/${role}.local.json`;
}

export function studioOidcRedirectUri(baseUrl = TestData.baseUrl): string {
  return `${baseUrl}${TestData.studioPath}/authentication/login-callback`;
}

export function studioAuthenticationLoginPath(baseUrl = TestData.baseUrl): string {
  return `${baseUrl}${TestData.studioPath}/authentication/login`;
}

const projectAuthMap: Record<string, { username: string; tenantName: string | null; role: AuthRole }> = {
  'chromium-admin': { username: TestData.users.hostAdmin, tenantName: null, role: 'admin' },
  'chromium-tenant-a-admin': {
    username: TestData.users.tenantAAdmin,
    tenantName: TestData.tenants.tenantA,
    role: 'tenant-a-admin',
  },
  'chromium-tenant-a-designer': {
    username: TestData.users.tenantADesigner,
    tenantName: TestData.tenants.tenantA,
    role: 'tenant-a-designer',
  },
  'chromium-tenant-b-admin': {
    username: TestData.users.tenantBAdmin,
    tenantName: TestData.tenants.tenantB,
    role: 'tenant-b-admin',
  },
};

export function getAuthForProject(projectName: string): {
  username: string;
  tenantName: string | null;
  role: AuthRole;
} {
  const auth = projectAuthMap[projectName];
  if (!auth) {
    throw new Error(`No E2E auth mapping for Playwright project '${projectName}'.`);
  }

  return auth;
}
