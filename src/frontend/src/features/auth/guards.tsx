import type { ReactNode } from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { selectIsAuthenticated, useAuthStore } from './authStore';

export function RequireAuth({ children }: { children: ReactNode }) {
  const isAuthenticated = useAuthStore(selectIsAuthenticated);
  const location = useLocation();

  if (!isAuthenticated) {
    return <Navigate to="/login" replace state={{ from: location }} />;
  }

  return <>{children}</>;
}

export function GuestOnly({ children }: { children: ReactNode }) {
  const isAuthenticated = useAuthStore(selectIsAuthenticated);

  if (isAuthenticated) {
    return <Navigate to="/" replace />;
  }

  return <>{children}</>;
}

/**
 * Gated by an RBAC permission (e.g. `users:write`) rather than a nav link, so the
 * route only renders when the caller holds the required capability. Permissions are
 * formatted `resource:action` (see `/api/auth/me`).
 */
export function RequirePermission({
  permission,
  children,
}: {
  permission: string;
  children: ReactNode;
}) {
  const permissions = useAuthStore((s) => s.permissions);
  const isAuthenticated = useAuthStore(selectIsAuthenticated);

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  if (!permissions.includes(permission)) {
    return <Navigate to="/" replace />;
  }

  return <>{children}</>;
}
