import { httpRequest } from '@/lib/http';

export interface CreateTenantRequest {
  name: string;
  country: string;
  currency: string;
  timezone?: string;
  locale?: string;
  adminEmail?: string;
  adminPassword?: string;
  adminName?: string;
}

export interface CreateTenantResponse {
  tenantId: string;
  name: string;
}

export function createTenant(
  accessToken: string,
  request: CreateTenantRequest,
): Promise<CreateTenantResponse> {
  return httpRequest<CreateTenantResponse>('/tenants', {
    method: 'POST',
    json: request,
    token: accessToken,
    idempotent: true,
    idempotencyKey: `tenant:${request.name.trim().toLowerCase()}`,
  });
}
