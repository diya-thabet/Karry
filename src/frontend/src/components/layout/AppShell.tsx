import { Outlet, useNavigate } from 'react-router-dom';
import { useAuthStore } from '@/features/auth/authStore';
import { logout } from '@/features/auth/api';

export function AppShell() {
  const navigate = useNavigate();
  const email = useAuthStore((s) => s.email);
  const roleCode = useAuthStore((s) => s.roleCode);
  const accessToken = useAuthStore((s) => s.accessToken);
  const refreshToken = useAuthStore((s) => s.refreshToken);
  const clear = useAuthStore((s) => s.clear);

  async function handleLogout() {
    try {
      if (refreshToken && accessToken) {
        await logout(refreshToken, accessToken);
      }
    } catch {
      // Local sign-out still proceeds even if the API is unreachable.
    } finally {
      clear();
      navigate('/login', { replace: true });
    }
  }

  return (
    <div className="flex min-h-screen flex-col">
      <header className="flex items-center justify-between bg-primary px-6 py-4 text-white">
        <div>
          <h1 className="text-xl font-bold">Karry Platform</h1>
          <p className="text-sm text-white/70">Enterprise Quarry &amp; Mining Management</p>
        </div>

        <div className="flex items-center gap-4">
          <div className="text-right">
            <p className="text-sm font-medium" data-testid="shell-email">
              {email ?? 'Signed in'}
            </p>
            <p className="text-xs text-white/70">{roleCode ? `Role: ${roleCode}` : '\u00A0'}</p>
          </div>
          <button
            onClick={handleLogout}
            className="rounded border border-white/40 px-3 py-1 text-sm text-white transition hover:bg-white/10"
          >
            Sign out
          </button>
        </div>
      </header>
      <main className="flex-1 px-6 py-6">
        <Outlet />
      </main>
    </div>
  );
}
