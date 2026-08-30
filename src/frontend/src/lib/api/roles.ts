import { httpRequest } from '@/lib/http';
import { toCreateRoleBody } from './contracts';
import type { CreateRoleBody } from './contracts';
import type { Role } from '@/features/auth/types';

export interface CreateRoleResponse {
  roleId: string;
  code: string;
}

export type CreateRoleRequest = CreateRoleBody;

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
    json: toCreateRoleBody(request),
    token: accessToken,
    idempotent: true,
    idempotencyKey: `role:${request.code.trim().toLowerCase()}`,
  });
}
