/** Mirrors host/BioTrace.Elsa.Abp.HttpApi.Host/Data/ElsaAbpMultiTenancySeedData.cs */
export const TestData = {
  password: '1q2w3E*',
  baseUrl: process.env.E2E_BASE_URL ?? 'https://localhost:44388',
  studioPath: '/studio',
  oidcClientId: 'ElsaStudio',
  oidcAuthority: process.env.E2E_BASE_URL ?? 'https://localhost:44388',

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
  },

  workflows: {
    demoTenantA: 'DemoTenantAWorkflow',
    demoTenantB: 'DemoTenantBWorkflow',
    e2eDelay: 'E2eDelayWorkflow',
  },
} as const;

export type AuthRole = 'admin' | 'tenant-a-admin' | 'tenant-a-designer' | 'tenant-b-admin';

export function authStoragePath(role: AuthRole): string {
  return `.auth/${role}.json`;
}
