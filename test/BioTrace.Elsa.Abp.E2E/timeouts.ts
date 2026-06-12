/**
 * Shared E2E timeouts — fail fast on CI; only bump when WASM/OIDC genuinely needs longer.
 */
export const E2E_TIMEOUTS = {
  test: 90_000,
  setupProject: 150_000,
  instanceCancelTest: 120_000,
  expect: 15_000,
  action: 15_000,
  navigation: 45_000,
  webServer: 120_000,
  wasmBoot: 45_000,
  loginFlow: 60_000,
  studioPage: 45_000,
  ui: 30_000,
  uiShort: 20_000,
  dialogClose: 45_000,
  workflowPoll: 60_000,
} as const;
