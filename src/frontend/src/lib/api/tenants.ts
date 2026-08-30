import { httpRequest } from '@/lib/http';
import { toCreateTenantBody } from './contracts';
import type { CreateTenantBody } from './contracts';

export interface CreateTenantResponse {
  tenantId: string;
  name: string;
}

export type CreateTenantRequest = CreateTenantBody;

export function createTenant(
  accessToken: string,
  request: CreateTenantRequest,
): Promise<CreateTenantResponse> {
  return httpRequest<CreateTenantResponse>('/tenants', {
    method: 'POST',
    json: toCreateTenantBody(request),
    token: accessToken,
    idempotent: true,
    idempotencyKey: `tenant:${request.name.trim().toLowerCase()}`,
  });
}
