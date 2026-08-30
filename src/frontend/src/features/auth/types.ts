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

export interface RefreshRequest {
  refreshToken: string;
  deviceId: string;
}

export interface RefreshResponse {
  accessToken: string;
  refreshToken: string;
  refreshTokenId: string;
}

export interface TwoFactorLoginRequest {
  challengeToken: string;
  code: string;
  deviceId: string;
}

export interface TwoFactorLoginResponse {
  tokens: TokenPair;
}

export interface CurrentUser {
  userId: string;
  email: string;
  roleCode: string | null;
}
