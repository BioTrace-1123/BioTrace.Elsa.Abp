import type { APIRequestContext, Page } from '@playwright/test';
import { getAccessToken } from './token';

export interface ElsaApiOptions {
  tenantName?: string | null;
}

export class ElsaApi {
  constructor(
    private readonly request: APIRequestContext,
    private readonly page: Page,
    private readonly options: ElsaApiOptions = {},
  ) {}

  private async headers(): Promise<Record<string, string>> {
    const token = await getAccessToken(this.page);
    const headers: Record<string, string> = {
      Authorization: `Bearer ${token}`,
      Accept: 'application/json',
      'Content-Type': 'application/json',
    };

    if (this.options.tenantName) {
      headers['__tenant'] = this.options.tenantName;
    }

    return headers;
  }

  async getCurrentUser(): Promise<{ permissions: string[] }> {
    const response = await this.request.get('/identity/users/me', {
      headers: await this.headers(),
    });

    if (!response.ok()) {
      throw new Error(`GET /identity/users/me failed: ${response.status()} ${await response.text()}`);
    }

    const body = (await response.json()) as { permissions?: string[] };
    return { permissions: body.permissions ?? [] };
  }

  async listWorkflowDefinitions(): Promise<Array<{ definitionId?: string; name?: string }>> {
    const response = await this.request.get('/elsa/api/workflow-definitions', {
      headers: await this.headers(),
    });

    if (!response.ok()) {
      throw new Error(`GET /elsa/api/workflow-definitions failed: ${response.status()} ${await response.text()}`);
    }

    const body = (await response.json()) as { items?: Array<{ definitionId?: string; name?: string }> };
    return body.items ?? [];
  }

  async createWorkflowDefinition(definitionId: string, name: string, publish = false): Promise<void> {
    const response = await this.request.post('/elsa/api/workflow-definitions', {
      headers: await this.headers(),
      data: {
        model: {
          definitionId,
          name,
          root: {
            type: 'Elsa.Sequence',
            id: 'root',
            version: 1,
          },
        },
        publish,
      },
    });

    if (!response.ok()) {
      throw new Error(`POST /elsa/api/workflow-definitions failed: ${response.status()} ${await response.text()}`);
    }
  }

  async publishWorkflowDefinition(definitionId: string): Promise<void> {
    const response = await this.request.post(`/elsa/api/workflow-definitions/${definitionId}/publish`, {
      headers: await this.headers(),
      data: {},
    });

    if (!response.ok()) {
      throw new Error(
        `POST /elsa/api/workflow-definitions/${definitionId}/publish failed: ${response.status()} ${await response.text()}`,
      );
    }
  }

  async executeWorkflowDefinition(definitionId: string): Promise<{ workflowInstanceId?: string; status?: string }> {
    const response = await this.request.post(`/elsa/api/workflow-definitions/${definitionId}/execute`, {
      headers: await this.headers(),
      data: {},
    });

    if (!response.ok()) {
      throw new Error(
        `POST /elsa/api/workflow-definitions/${definitionId}/execute failed: ${response.status()} ${await response.text()}`,
      );
    }

    return (await response.json()) as { workflowInstanceId?: string; status?: string };
  }

  async dispatchWorkflowDefinition(definitionId: string): Promise<{ workflowInstanceId?: string; status?: string }> {
    const response = await this.request.post(`/elsa/api/workflow-definitions/${definitionId}/dispatch`, {
      headers: await this.headers(),
      data: {},
    });

    if (!response.ok()) {
      throw new Error(
        `POST /elsa/api/workflow-definitions/${definitionId}/dispatch failed: ${response.status()} ${await response.text()}`,
      );
    }

    return (await response.json()) as { workflowInstanceId?: string; status?: string };
  }

  async getWorkflowInstance(instanceId: string): Promise<{ id?: string; status?: string }> {
    const response = await this.request.get(`/elsa/api/workflow-instances/${instanceId}`, {
      headers: await this.headers(),
    });

    if (!response.ok()) {
      throw new Error(
        `GET /elsa/api/workflow-instances/${instanceId} failed: ${response.status()} ${await response.text()}`,
      );
    }

    return (await response.json()) as { id?: string; status?: string };
  }

  async cancelWorkflowInstance(instanceId: string): Promise<void> {
    const response = await this.request.post(`/elsa/api/workflow-instances/${instanceId}/cancel`, {
      headers: await this.headers(),
      data: {},
    });

    if (!response.ok()) {
      throw new Error(
        `POST /elsa/api/workflow-instances/${instanceId}/cancel failed: ${response.status()} ${await response.text()}`,
      );
    }
  }

  async ensurePublishedDelayWorkflow(definitionId: string, delaySeconds = 60): Promise<void> {
    const definitions = await this.listWorkflowDefinitions();
    const existing = definitions.find((d) => d.definitionId === definitionId);
    if (existing) {
      return;
    }

    const duration = `00:00:${String(delaySeconds).padStart(2, '0')}`;
    const response = await this.request.post('/elsa/api/workflow-definitions', {
      headers: await this.headers(),
      data: {
        model: {
          definitionId,
          name: 'E2E Delay Workflow',
          root: {
            type: 'Elsa.Sequence',
            id: 'root',
            version: 1,
            activities: [
              {
                type: 'Elsa.Delay',
                id: 'delay-1',
                version: 1,
                duration: {
                  typeName: 'TimeSpan',
                  expression: {
                    type: 'Literal',
                    value: duration,
                  },
                },
              },
            ],
          },
        },
        publish: true,
      },
    });

    if (!response.ok()) {
      throw new Error(
        `Failed to seed delay workflow '${definitionId}': ${response.status()} ${await response.text()}`,
      );
    }
  }

  async listWorkflowInstances(definitionId?: string): Promise<Array<{ id?: string; status?: string; definitionId?: string }>> {
    const url = definitionId
      ? `/elsa/api/workflow-instances?definitionId=${encodeURIComponent(definitionId)}`
      : '/elsa/api/workflow-instances';

    const response = await this.request.get(url, {
      headers: await this.headers(),
    });

    if (!response.ok()) {
      throw new Error(`GET workflow-instances failed: ${response.status()} ${await response.text()}`);
    }

    const body = (await response.json()) as { items?: Array<{ id?: string; status?: string; definitionId?: string }> };
    return body.items ?? [];
  }
}

export function createElsaApi(page: Page, tenantName?: string | null): ElsaApi {
  return new ElsaApi(page.request, page, { tenantName });
}
