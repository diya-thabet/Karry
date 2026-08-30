import { Outlet } from 'react-router-dom';

export function AppShell() {
  return (
    <div className="flex min-h-screen flex-col">
      <header className="bg-primary px-6 py-4 text-white">
        <h1 className="text-xl font-bold">Karry Platform</h1>
        <p className="text-sm text-white/70">Enterprise Quarry &amp; Mining Management</p>
      </header>
      <main className="flex-1 px-6 py-6">
        <Outlet />
      </main>
    </div>
  );
}
