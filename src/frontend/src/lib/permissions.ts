/**
 * Mirrors the backend permission catalog (Karry.Domain.Identity). Claim strings are
 * formatted `resource:action` with lowercase enum names (read|write|mask).
 */
export const Perm = {
  Units: 'units',
  Tenants: 'tenants',
  Users: 'users',
  Roles: 'roles',
  Machines: 'machines',
  WearParts: 'wears',
} as const;

export const Action = {
  Read: 'read',
  Write: 'write',
  Mask: 'mask',
} as const;

/** A claim in `resource:action` form. */
export type PermissionClaim = `${string}:${string}`;

/** Returns true when `claim` (e.g. `users:write`) is present in the caller's set. */
export function can(claims: readonly string[], claim: PermissionClaim): boolean {
  return claims.includes(claim);
}
