import { httpRequest } from '@/lib/http';
import type { User } from '@/features/auth/types';

export interface CreateUserRequest {
  email: string;
  name: string;
  password: string;
  roleId: string;
}

export interface CreateUserResponse {
  userId: string;
  email: string;
}

export function listUsers(accessToken: string): Promise<User[]> {
  return httpRequest<User[]>('/users', {
    method: 'GET',
    token: accessToken,
  });
}

export function createUser(
  accessToken: string,
  request: CreateUserRequest,
): Promise<CreateUserResponse> {
  return httpRequest<CreateUserResponse>('/users', {
    method: 'POST',
    json: request,
    token: accessToken,
    idempotent: true,
    idempotencyKey: `user:${request.email.trim().toLowerCase()}`,
  });
}
