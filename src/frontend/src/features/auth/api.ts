import { httpRequest } from '@/lib/http';
import type {
  LoginRequest,
  LoginResponse,
  RefreshResponse,
  TwoFactorLoginRequest,
  TwoFactorLoginResponse,
} from './types';

const DEVICE_KEY = 'karry.deviceId';
export const TOKEN_STORAGE_KEY = 'karry.auth';

export function getDeviceId(): string {
  if (typeof localStorage === 'undefined') {
    return 'browser';
  }

  let id = localStorage.getItem(DEVICE_KEY);
  if (!id) {
    id =
      typeof crypto !== 'undefined' && 'randomUUID' in crypto
        ? crypto.randomUUID()
        : `${Date.now().toString(36)}-${Math.random().toString(36).slice(2)}`;
    localStorage.setItem(DEVICE_KEY, id);
  }

  return id;
}

export function login(request: LoginRequest): Promise<LoginResponse> {
  return httpRequest<LoginResponse>('/auth/login', {
    method: 'POST',
    json: request,
    idempotent: true,
    idempotencyKey: `login:${request.email.toLowerCase()}`,
  });
}

export function twoFactorLogin(request: TwoFactorLoginRequest): Promise<TwoFactorLoginResponse> {
  return httpRequest<TwoFactorLoginResponse>('/auth/two-factor/login', {
    method: 'POST',
    json: request,
    idempotent: true,
  });
}

export function refresh(refreshToken: string): Promise<RefreshResponse> {
  return httpRequest<RefreshResponse>('/auth/refresh', {
    method: 'POST',
    json: { refreshToken, deviceId: getDeviceId() },
    idempotent: true,
    idempotencyKey: `refresh:${refreshToken}`,
  });
}

export function logout(refreshToken: string, accessToken: string): Promise<void> {
  return httpRequest<void>('/auth/logout', {
    method: 'POST',
    json: { refreshToken, deviceId: getDeviceId() },
    token: accessToken,
    idempotent: true,
    idempotencyKey: `logout:${refreshToken}`,
  });
}

export function twoFactorEnable(accessToken: string): Promise<never> {
  return httpRequest('/auth/two-factor/enable', {
    method: 'POST',
    token: accessToken,
    idempotent: true,
  });
}
