/**
 * Pure request-body builders for the Karry API. Keeping these dependency-free
 * means every wire contract is directly unit-testable — and guards against
 * regressions exactly like the earlier incorrect 2FA login payload.
 */

export interface LoginBody {
  email: string;
  password: string;
  deviceId: string;
}

export function toLoginBody(email: string, password: string, deviceId: string): LoginBody {
  return { email: email.trim().toLowerCase(), password, deviceId };
}

export interface TwoFactorLoginBody {
  email: string;
  code: string;
  deviceId: string;
}

export function toTwoFactorLoginBody(
  email: string,
  code: string,
  deviceId: string,
): TwoFactorLoginBody {
  return { email: email.trim().toLowerCase(), code: code.trim(), deviceId };
}

export interface RefreshBody {
  refreshToken: string;
  deviceId: string;
}

export function toRefreshBody(refreshToken: string, deviceId: string): RefreshBody {
  return { refreshToken, deviceId };
}

export interface LogoutBody {
  refreshToken: string;
}

export function toLogoutBody(refreshToken: string): LogoutBody {
  return { refreshToken };
}

export interface EnableTwoFactorBody {
  deviceId: string;
}

export function toEnableTwoFactorBody(deviceId: string): EnableTwoFactorBody {
  return { deviceId };
}

export interface VerifyTwoFactorBody {
  secret: string;
  code: string;
}

export function toVerifyTwoFactorBody(secret: string, code: string): VerifyTwoFactorBody {
  return { secret, code: code.trim() };
}

export interface CreateTenantBody {
  name: string;
  country: string;
  currency: string;
  timezone?: string;
  locale?: string;
  adminEmail?: string;
  adminPassword?: string;
  adminName?: string;
}

export function toCreateTenantBody(input: CreateTenantBody): CreateTenantBody {
  return { ...input, name: input.name.trim() };
}

export interface CreateUserBody {
  email: string;
  name: string;
  password: string;
  roleId: string;
}

export function toCreateUserBody(input: CreateUserBody): CreateUserBody {
  return { ...input, email: input.email.trim().toLowerCase() };
}

export interface CreateRoleBody {
  code: string;
  name: string;
  description?: string | null;
}

export function toCreateRoleBody(input: CreateRoleBody): CreateRoleBody {
  return {
    code: input.code.trim().toLowerCase(),
    name: input.name.trim(),
    description: input.description?.trim() ? input.description.trim() : null,
  };
}

export interface SetUnitPreferencesBody {
  massUnit?: string | null;
  volumeUnit?: string | null;
}

export function toSetUnitPreferencesBody(input: SetUnitPreferencesBody): SetUnitPreferencesBody {
  return {
    massUnit: input.massUnit ?? null,
    volumeUnit: input.volumeUnit ?? null,
  };
}
