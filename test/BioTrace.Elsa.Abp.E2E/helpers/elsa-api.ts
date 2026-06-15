import { expect, type APIRequestContext } from '@playwright/test';
import { E2E_TIMEOUTS } from '../timeouts';
import { requestPasswordToken } from './token-client';

export interface ElsaApiOptions {
  username: string;
  tenantName?: string | null;
}

export class ElsaApi {
  private readonly tokenPromise: Promise<string>;

  constructor(
    private readonly request: APIRequestContext,
    private readonly options: ElsaApiOptions,
  ) {
    this.tokenPromise = requestPasswordToken(this.request, options.username, options.tenantName);
  }

  private async authHeaders(includeJsonContentType = false): Promise<Record<string, string>> {
    const token = await this.tokenPromise;
    const headers: Record<string, string> = {
      Authorization: `Bearer ${token}`,
      Accept: 'application/json',
    };

    if (includeJsonContentType) {
      headers['Content-Type'] = 'application/json';
    }

    if (this.options.tenantName) {
      headers['__tenant'] = this.options.tenantName;
    }

    return headers;
  }

  async getCurrentUser(): Promise<{ permissions: string[] }> {
    const response = await this.request.get('/identity/users/me', {
      headers: await this.authHeaders(),
    });

    if (!response.ok()) {
      throw new Error(`GET /identity/users/me failed: ${response.status()} ${await response.text()}`);
    }

    const body = (await response.json()) as { permissions?: string[] };
    return { permissions: body.permissions ?? [] };
  }

  async listWorkflowDefinitions(): Promise<Array<{ definitionId?: string; name?: string }>> {
    const response = await this.request.get('/elsa/api/workflow-definitions', {
      headers: await this.authHeaders(),
    });

    if (!response.ok()) {
      throw new Error(`GET /elsa/api/workflow-definitions failed: ${response.status()} ${await response.text()}`);
    }

    const body = (await response.json()) as { items?: Array<{ definitionId?: string; name?: string }> };
    return body.items ?? [];
  }

  async createWorkflowDefinition(definitionId: string, name: string, publish = false): Promise<void> {
    const response = await this.request.post('/elsa/api/workflow-definitions', {
      headers: await this.authHeaders(),
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
      headers: await this.authHeaders(),
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
      headers: await this.authHeaders(),
      data: {},
    });

    if (!response.ok()) {
      throw new Error(
        `POST /elsa/api/workflow-definitions/${definitionId}/execute failed: ${response.status()} ${await response.text()}`,
      );
    }

    return parseWorkflowExecutionResponse(await response.json());
  }

  async dispatchWorkflowDefinition(definitionId: string): Promise<{ workflowInstanceId?: string; status?: string }> {
    const response = await this.request.post(`/elsa/api/workflow-definitions/${definitionId}/dispatch`, {
      headers: await this.authHeaders(),
      data: {},
    });

    if (!response.ok()) {
      throw new Error(
        `POST /elsa/api/workflow-definitions/${definitionId}/dispatch failed: ${response.status()} ${await response.text()}`,
      );
    }

    const parsed = parseWorkflowExecutionResponse(await response.json());
    if (parsed.workflowInstanceId) {
      return parsed;
    }

    const instances = await this.listWorkflowInstances(definitionId);
    const running = instances.find((instance) => /running|executing|pending/i.test(instance.status ?? ''));
    if (running?.id) {
      return { workflowInstanceId: running.id, status: running.status };
    }

    throw new Error(
      `POST /elsa/api/workflow-definitions/${definitionId}/dispatch succeeded but no workflow instance id was returned.`,
    );
  }

  async waitForRunnableInstance(
    definitionId: string,
    preferredInstanceId?: string,
    timeoutMs = E2E_TIMEOUTS.workflowPoll,
  ): Promise<string> {
    let instanceId = preferredInstanceId ?? '';

    await expect
      .poll(
        async () => {
          if (instanceId) {
            try {
              const instance = await this.getWorkflowInstance(instanceId);
              if (instance.status && /running|executing|pending|suspended|scheduled|bookmarked/i.test(instance.status)) {
                return true;
              }
            } catch {
              instanceId = '';
            }
          }

          const instances = await this.listWorkflowInstances(definitionId);
          const match = instances.find(
            (instance) =>
              instance.id
              && instance.status
              && /running|executing|pending|suspended|scheduled|bookmarked/i.test(instance.status),
          );
          instanceId = match?.id ?? '';
          return instanceId.length > 0;
        },
        { timeout: timeoutMs, intervals: [2_000] },
      )
      .toBe(true);

    return instanceId;
  }

  async getWorkflowInstance(instanceId: string): Promise<{ id?: string; status?: string; subStatus?: string }> {
    const response = await this.request.get(`/elsa/api/workflow-instances/${instanceId}`, {
      headers: await this.authHeaders(),
    });

    if (!response.ok()) {
      throw new Error(
        `GET /elsa/api/workflow-instances/${instanceId} failed: ${response.status()} ${await response.text()}`,
      );
    }

    return (await response.json()) as { id?: string; status?: string; subStatus?: string };
  }

  instanceStatusText(instance: { status?: string; subStatus?: string }): string {
    return [instance.subStatus, instance.status].filter(Boolean).join(' ');
  }

  async waitForInstanceStatus(instanceId: string, statusPattern: RegExp, timeoutMs = E2E_TIMEOUTS.workflowPoll): Promise<void> {
    await expect
      .poll(
        async () => {
          try {
            const instance = await this.getWorkflowInstance(instanceId);
            return statusPattern.test(this.instanceStatusText(instance));
          } catch {
            return false;
          }
        },
        { timeout: timeoutMs, intervals: [2_000] },
      )
      .toBe(true);
  }

  async cancelWorkflowInstance(instanceId: string): Promise<void> {
    const response = await this.request.post(`/elsa/api/cancel/workflow-instances/${instanceId}`, {
      headers: await this.authHeaders(),
      data: {},
    });

    if (!response.ok()) {
      throw new Error(
        `POST /elsa/api/cancel/workflow-instances/${instanceId} failed: ${response.status()} ${await response.text()}`,
      );
    }
  }

  async ensurePublishedDelayWorkflow(definitionId: string, delaySeconds = 300): Promise<void> {
    const hours = Math.floor(delaySeconds / 3600);
    const minutes = Math.floor((delaySeconds % 3600) / 60);
    const seconds = delaySeconds % 60;
    const duration = `${String(hours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`;
    const response = await this.request.post('/elsa/api/workflow-definitions', {
      headers: await this.authHeaders(),
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
                timeSpan: {
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
      headers: await this.authHeaders(),
    });

    if (!response.ok()) {
      throw new Error(`GET workflow-instances failed: ${response.status()} ${await response.text()}`);
    }

    const body = (await response.json()) as { items?: Array<{ id?: string; status?: string; definitionId?: string }> };
    return body.items ?? [];
  }
}

function parseWorkflowExecutionResponse(body: unknown): { workflowInstanceId?: string; status?: string } {
  if (!body || typeof body !== 'object') {
    return {};
  }

  const record = body as Record<string, unknown>;
  const workflowState = record.workflowState as Record<string, unknown> | undefined;

  return {
    workflowInstanceId:
      (record.workflowInstanceId as string | undefined)
      ?? (record.id as string | undefined)
      ?? (record.instanceId as string | undefined)
      ?? (workflowState?.id as string | undefined),
    status:
      (record.status as string | undefined)
      ?? (workflowState?.status as string | undefined),
  };
}

export function createElsaApi(
  request: APIRequestContext,
  username: string,
  tenantName?: string | null,
): ElsaApi {
  return new ElsaApi(request, { username, tenantName });
}
