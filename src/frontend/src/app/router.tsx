import { createBrowserRouter } from 'react-router-dom';
import { AppShell } from '@/components/layout/AppShell';
import { HomePage } from '@/features/home/HomePage';
import { UsersPage } from '@/features/users/UsersPage';
import { RolesPage } from '@/features/roles/RolesPage';
import { UnitPreferencesPage } from '@/features/units/UnitPreferencesPage';
import { TenantsPage } from '@/features/tenants/TenantsPage';
import { SecurityPage } from '@/features/security/SecurityPage';
import { LoginPage } from '@/features/auth/LoginPage';
import { GuestOnly, RequireAuth, RequirePermission } from '@/features/auth/guards';

export const AppRouter = createBrowserRouter([
  {
    path: '/login',
    element: (
      <GuestOnly>
        <LoginPage />
      </GuestOnly>
    ),
  },
  {
    path: '/',
    element: (
      <RequireAuth>
        <AppShell />
      </RequireAuth>
    ),
    children: [
      { index: true, element: <HomePage /> },
      {
        path: 'users',
        element: (
          <RequirePermission permission="users:read">
            <UsersPage />
          </RequirePermission>
        ),
      },
      {
        path: 'roles',
        element: (
          <RequirePermission permission="roles:read">
            <RolesPage />
          </RequirePermission>
        ),
      },
      { path: 'units', element: <UnitPreferencesPage /> },
      {
        path: 'tenants',
        element: (
          <RequirePermission permission="tenants:write">
            <TenantsPage />
          </RequirePermission>
        ),
      },
      { path: 'security', element: <SecurityPage /> },
    ],
  },
]);
