import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import type { TokenPair } from './types';

export interface AuthState {
  accessToken: string | null;
  refreshToken: string | null;
  refreshTokenId: string | null;
  userId: string | null;
  roleCode: string | null;
  email: string | null;
  setSession: (
    tokens: TokenPair,
    userId: string | null,
    roleCode: string | null,
    email: string | null,
  ) => void;
  updateTokens: (tokens: TokenPair) => void;
  setEmail: (email: string) => void;
  clear: () => void;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      accessToken: null,
      refreshToken: null,
      refreshTokenId: null,
      userId: null,
      roleCode: null,
      email: null,

      setSession: (tokens, userId, roleCode, email) =>
        set({
          accessToken: tokens.accessToken,
          refreshToken: tokens.refreshToken,
          refreshTokenId: tokens.refreshTokenId,
          userId,
          roleCode,
          email,
        }),

      updateTokens: (tokens) =>
        set({
          accessToken: tokens.accessToken,
          refreshToken: tokens.refreshToken,
          refreshTokenId: tokens.refreshTokenId,
        }),

      setEmail: (email) => set({ email }),

      clear: () =>
        set({
          accessToken: null,
          refreshToken: null,
          refreshTokenId: null,
          userId: null,
          roleCode: null,
          email: null,
        }),
    }),
    {
      name: 'karry.auth',
      partialize: (state) => ({
        accessToken: state.accessToken,
        refreshToken: state.refreshToken,
        refreshTokenId: state.refreshTokenId,
        userId: state.userId,
        roleCode: state.roleCode,
        email: state.email,
      }),
    },
  ),
);

export function selectIsAuthenticated(state: AuthState): boolean {
  return Boolean(state.accessToken && state.refreshToken);
}
