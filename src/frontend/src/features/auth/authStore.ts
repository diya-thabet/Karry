import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import type { CurrentSession, TokenPair } from './types';

export interface AuthState {
  accessToken: string | null;
  refreshToken: string | null;
  refreshTokenId: string | null;
  userId: string | null;
  email: string | null;
  name: string | null;
  roleCode: string | null;
  tenantId: string | null;
  isPlatformAdmin: boolean;
  twoFactorEnabled: boolean;
  permissions: string[];
  setTokens: (
    tokens: TokenPair,
    session: { userId: string | null; roleCode: string | null; email: string | null },
  ) => void;
  setCurrentSession: (session: CurrentSession) => void;
  clear: () => void;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      accessToken: null,
      refreshToken: null,
      refreshTokenId: null,
      userId: null,
      email: null,
      name: null,
      roleCode: null,
      tenantId: null,
      isPlatformAdmin: false,
      twoFactorEnabled: false,
      permissions: [],

      setTokens: (tokens, session) =>
        set({
          accessToken: tokens.accessToken,
          refreshToken: tokens.refreshToken,
          refreshTokenId: tokens.refreshTokenId,
          userId: session.userId,
          email: session.email,
          roleCode: session.roleCode,
        }),

      setCurrentSession: (session) =>
        set({
          userId: session.userId,
          email: session.email,
          name: session.name,
          tenantId: session.tenantId,
          roleCode: session.roleCode,
          isPlatformAdmin: session.isPlatformAdmin,
          twoFactorEnabled: session.twoFactorEnabled,
          permissions: session.permissions,
        }),

      clear: () =>
        set({
          accessToken: null,
          refreshToken: null,
          refreshTokenId: null,
          userId: null,
          email: null,
          name: null,
          roleCode: null,
          tenantId: null,
          isPlatformAdmin: false,
          twoFactorEnabled: false,
          permissions: [],
        }),
    }),
    {
      name: 'karry.auth',
      partialize: (state) => ({
        accessToken: state.accessToken,
        refreshToken: state.refreshToken,
        refreshTokenId: state.refreshTokenId,
        userId: state.userId,
        email: state.email,
        name: state.name,
        roleCode: state.roleCode,
        tenantId: state.tenantId,
        isPlatformAdmin: state.isPlatformAdmin,
        twoFactorEnabled: state.twoFactorEnabled,
        permissions: state.permissions,
      }),
    },
  ),
);

export function selectIsAuthenticated(state: AuthState): boolean {
  return Boolean(state.accessToken && state.refreshToken);
}
