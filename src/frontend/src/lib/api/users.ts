import { httpRequest } from '@/lib/http';
import { toCreateUserBody } from './contracts';
import type { CreateUserBody } from './contracts';
import type { User } from '@/features/auth/types';

export interface CreateUserResponse {
  userId: string;
  email: string;
}

export type CreateUserRequest = CreateUserBody;

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
    json: toCreateUserBody(request),
    token: accessToken,
    idempotent: true,
    idempotencyKey: `user:${request.email.trim().toLowerCase()}`,
  });
}
