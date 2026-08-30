import { useAuthStore } from './authStore';
import { refresh } from '@/lib/api';

let refreshInFlight: Promise<string | null> | null = null;

/**
 * Returns a fresh access token. If one is cached, returns it. Otherwise a single
 * in-flight refresh is shared by all callers; a failed/rotated refresh clears the
 * session to satisfy the server's reuse-detection (any attempt to reuse an older
 * refresh token revokes the whole family).
 */
export async function getAccessToken(): Promise<string | null> {
  const { accessToken, refreshToken } = useAuthStore.getState();

  if (accessToken) {
    return accessToken;
  }

  if (!refreshToken) {
    return null;
  }

  if (!refreshInFlight) {
    refreshInFlight = doRefresh(refreshToken).finally(() => {
      refreshInFlight = null;
    });
  }

  return refreshInFlight;
}

async function doRefresh(refreshToken: string): Promise<string | null> {
  try {
    const pair = await refresh(refreshToken);
    const current = useAuthStore.getState();
    current.setTokens(pair, {
      userId: current.userId,
      roleCode: current.roleCode,
      email: current.email,
    });
    return pair.accessToken;
  } catch {
    useAuthStore.getState().clear();
    return null;
  }
}
