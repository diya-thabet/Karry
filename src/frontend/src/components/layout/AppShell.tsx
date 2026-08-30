import { useEffect, useState } from 'react';
import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { cn } from '@/lib/cn';
import { useAuth } from '@/features/auth/useAuth';
import { useAuthStore } from '@/features/auth/authStore';
import { logout } from '@/lib/api';
import { Avatar } from '@/components/ui/Avatar';
import { Badge } from '@/components/ui/Badge';
import type { PermissionClaim } from '@/lib/permissions';

interface NavItem {
  to: string;
  label: string;
  icon: React.ReactNode;
  /** Permission required to see the item; platform admins always see everything. */
  permission?: PermissionClaim;
  /** Only visible to platform admins (no tenant). */
  platformAdminOnly?: boolean;
}

function DashboardIcon() {
  return (
    <svg className="h-5 w-5" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
      <path d="M3 3h6v6H3V3Zm8 0h6v4h-6V3Zm0 6h6v8h-6V9Zm-8 2h6v6H3v-6Z" />
    </svg>
  );
}

function UsersIcon() {
  return (
    <svg className="h-5 w-5" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
      <path d="M10 9a3 3 0 1 0 0-6 3 3 0 0 0 0 6Zm-6 8a6 6 0 1 1 12 0v.5H4V17Z" />
    </svg>
  );
}

function ShieldIcon() {
  return (
    <svg className="h-5 w-5" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
      <path d="M10 2 3 4.5V10c0 4 3 6.8 7 8 4-1.2 7-4 7-8V4.5L10 2Zm0 3.5V16c-2.6-.8-4.5-2.9-4.7-5.3h4.7V5.5Z" />
    </svg>
  );
}

function RulerIcon() {
  return (
    <svg className="h-5 w-5" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
      <path d="M15.5 4 18 6.5l-1 1-1.2-1.2-1 1 1.2 1.2-1 1-1.2-1.2-1 1L13 10l-2.2-2.2 1-1-1.2-1.2-1 1 1.2 1.2-1 1-1.2-1.2-1 1L6.5 8 4 5.5 5.5 4h10Z" />
    </svg>
  );
}

function BuildingIcon() {
  return (
    <svg className="h-5 w-5" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
      <path d="M6 2h8v2h2v14h2v2H2v-2h2V4h2V2Zm1 3v2h2V5H7Zm4 0v2h2V5h-2ZM7 9v2h2V9H7Zm4 0v2h2V9h-2Zm-4 4v2h2v-2H7Zm4 0v2h2v-2h-2Z" />
    </svg>
  );
}

function KeyIcon() {
  return (
    <svg className="h-5 w-5" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
      <path d="M14.5 2a3.5 3.5 0 0 0-3.2 4.9L3.6 14.6a1 1 0 0 0-.3.7V18h3v-2h2v-2h2l1.6-1.6A3.5 3.5 0 1 0 14.5 2Zm0 5a1.5 1.5 0 1 1 0-3 1.5 1.5 0 0 1 0 3Z" />
    </svg>
  );
}

function SignOutIcon() {
  return (
    <svg className="h-5 w-5" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
      <path d="M11 3H4v14h7v-2H6V5h5V3Zm2 3-1.4 1.4L14.2 10H8v2h6.2l-2.6 2.6L13 16l5-5-5-5Z" />
    </svg>
  );
}

const NAV: NavItem[] = [
  { to: '/', label: 'Dashboard', icon: <DashboardIcon /> },
  { to: '/users', label: 'Users', icon: <UsersIcon />, permission: 'users:read' },
  { to: '/roles', label: 'Roles', icon: <ShieldIcon />, permission: 'roles:read' },
  { to: '/units', label: 'Unit Preferences', icon: <RulerIcon />, permission: 'units:read' },
  { to: '/tenants', label: 'Tenants', icon: <BuildingIcon />, platformAdminOnly: true },
  { to: '/security', label: 'Security', icon: <KeyIcon /> },
];

function SidebarContent({
  onNavigate,
  isPlatformAdmin,
  permissions,
}: {
  onNavigate?: () => void;
  isPlatformAdmin: boolean;
  permissions: string[];
}) {
  const visible = NAV.filter((item) => {
    if (item.platformAdminOnly) return isPlatformAdmin;
    if (item.permission && !permissions.includes(item.permission) && !isPlatformAdmin) return false;
    return true;
  });

  return (
    <nav className="flex-1 space-y-1 px-3 py-4">
      {visible.map((item) => (
        <NavLink
          key={item.to}
          to={item.to}
          end={item.to === '/'}
          onClick={onNavigate}
          className={({ isActive }) =>
            cn(
              'flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium transition-colors',
              isActive
                ? 'bg-white/12 text-white'
                : 'text-white/70 hover:bg-white/8 hover:text-white',
            )
          }
        >
          {item.icon}
          <span>{item.label}</span>
        </NavLink>
      ))}
    </nav>
  );
}

export function AppShell() {
  const navigate = useNavigate();
  const { email, name, roleCode, isPlatformAdmin, permissions, refreshSession, isAuthenticated } =
    useAuth();
  const accessToken = useAuthStoreValue('accessToken');
  const refreshToken = useAuthStoreValue('refreshToken');
  const clear = useAuthStoreClear();
  const [menuOpen, setMenuOpen] = useState(false);

  useEffect(() => {
    if (isAuthenticated) {
      void refreshSession().catch(() => undefined);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  async function handleSignOut() {
    try {
      if (refreshToken && accessToken) {
        await logout(refreshToken, accessToken);
      }
    } catch {
      // Local sign-out still proceeds if the API is unreachable.
    } finally {
      clear();
      navigate('/login', { replace: true });
    }
  }

  const displayName = name ?? email ?? 'Signed in';
  const shell = (
    <div className="flex h-14 shrink-0 items-center justify-between gap-3 px-5">
      <div className="flex items-center gap-2.5">
        <Avatar name={displayName} />
        <div className="min-w-0 leading-tight">
          <p className="truncate text-sm font-semibold text-white">{displayName}</p>
          <p className="truncate text-xs text-white/60">
            {isPlatformAdmin ? 'Platform Admin' : (roleCode ?? 'Guest')}
          </p>
        </div>
      </div>
      <button
        onClick={handleSignOut}
        title="Sign out"
        className="focus-ring rounded-lg p-2 text-white/70 transition-colors hover:bg-white/10 hover:text-white"
      >
        <SignOutIcon />
      </button>
    </div>
  );

  return (
    <div className="flex min-h-screen">
      {/* Sidebar (desktop) */}
      <aside className="hidden w-64 shrink-0 flex-col bg-primary lg:flex">
        <div className="flex h-16 items-center gap-2 px-5">
          <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-white/10 font-black text-white">
            K
          </div>
          <div className="leading-tight">
            <p className="text-sm font-bold text-white">Karry</p>
            <p className="text-[11px] text-white/60">Quarry &amp; Mining OS</p>
          </div>
        </div>
        <SidebarContent isPlatformAdmin={isPlatformAdmin} permissions={permissions} />
        <div className="border-t border-white/10">{shell}</div>
      </aside>

      {/* Mobile top bar */}
      <div className="flex min-w-0 flex-1 flex-col">
        <header className="flex h-14 items-center justify-between gap-3 bg-primary px-4 lg:hidden">
          <div className="flex items-center gap-2">
            <div className="flex h-7 w-7 items-center justify-center rounded-lg bg-white/10 font-black text-white">
              K
            </div>
            <span className="text-sm font-bold text-white">Karry</span>
          </div>
          <div className="flex items-center gap-2">
            {isPlatformAdmin && <Badge tone="info">Platform</Badge>}
            <button
              onClick={() => setMenuOpen((v) => !v)}
              aria-label="Toggle navigation"
              aria-expanded={menuOpen}
              className="focus-ring rounded-lg p-2 text-white hover:bg-white/10"
            >
              <svg
                className="h-6 w-6"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="2"
                aria-hidden="true"
              >
                <path d="M4 6h16M4 12h16M4 18h16" strokeLinecap="round" />
              </svg>
            </button>
          </div>
        </header>

        {/* Mobile nav drawer */}
        <div className={cn('lg:hidden', menuOpen ? 'block' : 'hidden')}>
          <SidebarContent
            isPlatformAdmin={isPlatformAdmin}
            permissions={permissions}
            onNavigate={() => setMenuOpen(false)}
          />
        </div>

        <main className="flex-1 px-4 py-6 sm:px-6 lg:px-8">
          <Outlet />
        </main>
      </div>

      {/* Mobile sign-out footer */}
      {menuOpen && (
        <div className="fixed inset-x-0 bottom-0 border-t border-white/10 bg-primary lg:hidden">
          {shell}
        </div>
      )}
    </div>
  );
}

// Small selector helpers to keep the render stable.
function useAuthStoreValue<K extends keyof ReturnType<typeof useAuthStore.getState>>(key: K) {
  return useAuthStore((s) => s[key]);
}

function useAuthStoreClear() {
  return useAuthStore((s) => s.clear);
}
