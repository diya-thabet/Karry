import { httpRequest } from '@/lib/http';
import {
  toEnableTwoFactorBody,
  toLoginBody,
  toLogoutBody,
  toRefreshBody,
  toTwoFactorLoginBody,
  toVerifyTwoFactorBody,
} from './contracts';
import type {
  CurrentSession,
  EnableTwoFactorResponse,
  LoginRequest,
  LoginResponse,
  RefreshResponse,
  TwoFactorLoginRequest,
} from '@/features/auth/types';

const DEVICE_KEY = 'karry.deviceId';

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
    json: toLoginBody(request.email, request.password, request.deviceId),
    idempotent: true,
    idempotencyKey: `login:${request.email.toLowerCase()}`,
  });
}

export function twoFactorLogin(request: TwoFactorLoginRequest): Promise<LoginResponse> {
  return httpRequest<LoginResponse>('/auth/two-factor/login', {
    method: 'POST',
    json: toTwoFactorLoginBody(request.email, request.code, request.deviceId),
    idempotent: true,
  });
}

export function refresh(refreshToken: string): Promise<RefreshResponse> {
  return httpRequest<RefreshResponse>('/auth/refresh', {
    method: 'POST',
    json: toRefreshBody(refreshToken, getDeviceId()),
    idempotent: true,
    idempotencyKey: `refresh:${refreshToken}`,
  });
}

export function logout(refreshToken: string, accessToken: string): Promise<void> {
  return httpRequest<void>('/auth/logout', {
    method: 'POST',
    json: toLogoutBody(refreshToken),
    token: accessToken,
    idempotent: true,
    idempotencyKey: `logout:${refreshToken}`,
  });
}

export function getCurrentSession(accessToken: string): Promise<CurrentSession> {
  return httpRequest<CurrentSession>('/auth/me', {
    method: 'GET',
    token: accessToken,
  });
}

export function enableTwoFactor(accessToken: string): Promise<EnableTwoFactorResponse> {
  return httpRequest<EnableTwoFactorResponse>('/auth/two-factor/enable', {
    method: 'POST',
    json: toEnableTwoFactorBody(getDeviceId()),
    token: accessToken,
    idempotent: true,
  });
}

export function verifyTwoFactor(accessToken: string, secret: string, code: string): Promise<void> {
  return httpRequest<void>('/auth/two-factor/verify', {
    method: 'POST',
    json: toVerifyTwoFactorBody(secret, code),
    token: accessToken,
    idempotent: true,
  });
}

export function disableTwoFactor(accessToken: string): Promise<void> {
  return httpRequest<void>('/auth/two-factor/disable', {
    method: 'POST',
    json: {},
    token: accessToken,
    idempotent: true,
  });
}
