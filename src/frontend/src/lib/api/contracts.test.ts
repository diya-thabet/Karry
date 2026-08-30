import { describe, expect, it } from 'vitest';
import {
  toCreateRoleBody,
  toCreateTenantBody,
  toCreateUserBody,
  toEnableTwoFactorBody,
  toLoginBody,
  toLogoutBody,
  toRefreshBody,
  toSetUnitPreferencesBody,
  toTwoFactorLoginBody,
  toVerifyTwoFactorBody,
} from './contracts';

describe('toLoginBody', () => {
  it('normalises email to trimmed lowercase', () => {
    expect(toLoginBody('  User@Example.COM ', 'pw', 'dev-1')).toEqual({
      email: 'user@example.com',
      password: 'pw',
      deviceId: 'dev-1',
    });
  });

  it('passes through password and deviceId unchanged', () => {
    const body = toLoginBody('a@b.c', 'Secret 1', 'device');
    expect(body.password).toBe('Secret 1');
    expect(body.deviceId).toBe('device');
  });
});

describe('toTwoFactorLoginBody', () => {
  it('builds the email + code + deviceId contract (regression guard for the 2FA bug)', () => {
    expect(toTwoFactorLoginBody('  A@B.C', ' 123456 ', 'dev-x')).toEqual({
      email: 'a@b.c',
      code: '123456',
      deviceId: 'dev-x',
    });
  });

  it('trims the verification code when present', () => {
    expect(toTwoFactorLoginBody('a@b.c', '  111222  ', 'd').code).toBe('111222');
  });
});

describe('toRefreshBody & toLogoutBody', () => {
  it('refresh carries refreshToken and deviceId', () => {
    expect(toRefreshBody('tok', 'dev')).toEqual({ refreshToken: 'tok', deviceId: 'dev' });
  });

  it('logout carries only the refresh token', () => {
    expect(toLogoutBody('tok')).toEqual({ refreshToken: 'tok' });
  });
});

describe('toEnableTwoFactorBody & toVerifyTwoFactorBody', () => {
  it('enable sends the device id', () => {
    expect(toEnableTwoFactorBody('dev-9')).toEqual({ deviceId: 'dev-9' });
  });

  it('verify sends secret with a trimmed code', () => {
    expect(toVerifyTwoFactorBody('BASE32SECRET', ' 000111 ')).toEqual({
      secret: 'BASE32SECRET',
      code: '000111',
    });
  });
});

describe('toCreateTenantBody', () => {
  it('trims the tenant name and preserves the rest', () => {
    expect(
      toCreateTenantBody({
        name: '  Acme Quarries  ',
        country: 'KE',
        currency: 'KES',
        timezone: 'Africa/Nairobi',
        locale: 'en',
      }),
    ).toEqual({
      name: 'Acme Quarries',
      country: 'KE',
      currency: 'KES',
      timezone: 'Africa/Nairobi',
      locale: 'en',
    });
  });

  it('preserves optional admin fields when provided', () => {
    const body = toCreateTenantBody({
      name: 'T',
      country: 'KE',
      currency: 'USD',
      adminEmail: 'a@b.c',
      adminPassword: 'x',
      adminName: 'A B',
    });
    expect(body.adminEmail).toBe('a@b.c');
    expect(body.adminPassword).toBe('x');
    expect(body.adminName).toBe('A B');
  });
});

describe('toCreateUserBody', () => {
  it('normalises email to trimmed lowercase', () => {
    expect(
      toCreateUserBody({ email: '  X@Y.Z ', name: 'X Y', password: 'pw', roleId: 'r1' }),
    ).toEqual({
      email: 'x@y.z',
      name: 'X Y',
      password: 'pw',
      roleId: 'r1',
    });
  });
});

describe('toCreateRoleBody', () => {
  it('normalises code to lowercase and trims name', () => {
    expect(toCreateRoleBody({ code: ' OPERATOR ', name: '  Operator  ' })).toEqual({
      code: 'operator',
      name: 'Operator',
      description: null,
    });
  });

  it('keeps a non-empty description', () => {
    expect(toCreateRoleBody({ code: 'op', name: 'Op', description: '  Runs machines  ' })).toEqual({
      code: 'op',
      name: 'Op',
      description: 'Runs machines',
    });
  });

  it('coerces blank description to null', () => {
    expect(toCreateRoleBody({ code: 'op', name: 'Op', description: '   ' })).toEqual({
      code: 'op',
      name: 'Op',
      description: null,
    });
  });
});

describe('toSetUnitPreferencesBody', () => {
  it('passes values through', () => {
    expect(toSetUnitPreferencesBody({ massUnit: 't', volumeUnit: 'm3' })).toEqual({
      massUnit: 't',
      volumeUnit: 'm3',
    });
  });

  it('defaults missing units to null', () => {
    expect(toSetUnitPreferencesBody({})).toEqual({ massUnit: null, volumeUnit: null });
  });
});
