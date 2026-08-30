import { describe, expect, it } from 'vitest';
import { Action, can, Perm } from './permissions';

describe('can', () => {
  it('returns true when the claim is present', () => {
    expect(can(['users:read', 'users:write'], 'users:write')).toBe(true);
  });

  it('returns false when the claim is absent', () => {
    expect(can(['users:read'], 'users:write')).toBe(false);
  });

  it('is case-sensitive and exact', () => {
    expect(can(['Users:write', 'users:writee'], 'users:write')).toBe(false);
  });

  it('works with catalog constants', () => {
    const claims = [`${Perm.Users}:${Action.Read}`, `${Perm.Tenants}:${Action.Write}`];
    expect(can(claims, 'users:read')).toBe(true);
    expect(can(claims, 'tenants:write')).toBe(true);
    expect(can(claims, 'roles:write')).toBe(false);
  });

  it('handles empty claim sets', () => {
    expect(can([], 'users:read')).toBe(false);
  });

  it('matches exact string length with colons', () => {
    expect(can(['a:b'], 'a:b')).toBe(true);
    expect(can(['a:b '], 'a:b')).toBe(false);
  });

  it('is case-sensitive about action part', () => {
    expect(can(['users:Read'], 'users:read')).toBe(false);
    expect(can(['users:write'], 'users:Write')).toBe(false);
  });
});
