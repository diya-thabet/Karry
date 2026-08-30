import { useCallback } from 'react';
import { useAuthStore } from './authStore';
import { getCurrentSession } from '@/lib/api';
import { getAccessToken } from './tokenManager';

export interface UseAuthResult {
  userId: string | null;
  email: string | null;
  name: string | null;
  roleCode: string | null;
  tenantId: string | null;
  isPlatformAdmin: boolean;
  twoFactorEnabled: boolean;
  permissions: string[];
  isAuthenticated: boolean;
  /** Re-fetches the session from the API, updating the store. */
  refreshSession: () => Promise<CurrentSessionish | null>;
}

// Local structural type to avoid importing CurrentSession directly here (kept light).
type CurrentSessionish = {
  userId: string;
  email: string;
  name: string;
  tenantId: string | null;
  roleCode: string | null;
  isPlatformAdmin: boolean;
  twoFactorEnabled: boolean;
  permissions: string[];
};

export function useAuth(): UseAuthResult {
  const state = useAuthStore();
  const setCurrentSession = useAuthStore((s) => s.setCurrentSession);

  const refreshSession = useCallback(async () => {
    const token = await getAccessToken();
    if (!token) return null;
    const session = await getCurrentSession(token);
    setCurrentSession(session);
    return session;
  }, [setCurrentSession]);

  return {
    userId: state.userId,
    email: state.email,
    name: state.name,
    roleCode: state.roleCode,
    tenantId: state.tenantId,
    isPlatformAdmin: state.isPlatformAdmin,
    twoFactorEnabled: state.twoFactorEnabled,
    permissions: state.permissions,
    isAuthenticated: Boolean(state.accessToken && state.refreshToken),
    refreshSession,
  };
}
