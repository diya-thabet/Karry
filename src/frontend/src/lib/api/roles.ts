import { httpRequest } from '@/lib/http';
import type { Role } from '@/features/auth/types';

export interface CreateRoleRequest {
  code: string;
  name: string;
  description?: string | null;
}

export interface CreateRoleResponse {
  roleId: string;
  code: string;
}

export function listRoles(accessToken: string): Promise<Role[]> {
  return httpRequest<Role[]>('/roles', {
    method: 'GET',
    token: accessToken,
  });
}

export function createRole(
  accessToken: string,
  request: CreateRoleRequest,
): Promise<CreateRoleResponse> {
  return httpRequest<CreateRoleResponse>('/roles', {
    method: 'POST',
    json: request,
    token: accessToken,
    idempotent: true,
    idempotencyKey: `role:${request.code.trim().toLowerCase()}`,
  });
}
