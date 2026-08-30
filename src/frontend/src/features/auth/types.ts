export interface TokenPair {
  accessToken: string;
  refreshToken: string;
  refreshTokenId: string;
}

export interface LoginRequest {
  email: string;
  password: string;
  deviceId: string;
}

export interface LoginResponse {
  requiresTwoFactor: boolean;
  challengeToken: string | null;
  tokens: TokenPair | null;
  userId: string | null;
  roleCode: string | null;
  twoFactorProvisioningUri: string | null;
}

export interface TwoFactorLoginRequest {
  email: string;
  code: string;
  deviceId: string;
}

export interface RefreshRequest {
  refreshToken: string;
  deviceId: string;
}

export type RefreshResponse = TokenPair;

export interface CurrentSession {
  userId: string;
  email: string;
  name: string;
  tenantId: string | null;
  roleCode: string | null;
  isPlatformAdmin: boolean;
  twoFactorEnabled: boolean;
  permissions: string[];
}

export interface EnableTwoFactorResponse {
  secret: string;
  provisioningUri: string;
}

export interface User {
  userId: string;
  email: string;
  name: string;
  isActive: boolean;
  twoFactorEnabled: boolean;
  roleId: string | null;
  createdAtUtc: string;
  roleCode: string | null;
}

export interface Role {
  roleId: string;
  code: string;
  name: string;
  description: string | null;
  permissions: string[];
}
