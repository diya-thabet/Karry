import { FormEvent, useState } from 'react';
import { Navigate, useLocation, useNavigate } from 'react-router-dom';
import { selectIsAuthenticated, useAuthStore } from './authStore';
import { getDeviceId, login, twoFactorLogin } from './api';
import type { LoginResponse } from './types';

export function LoginPage() {
  const isAuthenticated = useAuthStore(selectIsAuthenticated);
  const setSession = useAuthStore((s) => s.setSession);
  const navigate = useNavigate();
  const location = useLocation();

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [twoFactorCode, setTwoFactorCode] = useState('');
  const [challenge, setChallenge] = useState<LoginResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const from = (location.state as { from?: { pathname?: string } } | null)?.from?.pathname ?? '/';

  if (isAuthenticated) {
    return <Navigate to="/" replace />;
  }

  async function handleLogin(event: FormEvent) {
    event.preventDefault();
    setError(null);
    setLoading(true);

    try {
      if (challenge) {
        const response = await twoFactorLogin({
          challengeToken: challenge.challengeToken ?? '',
          code: twoFactorCode,
          deviceId: getDeviceId(),
        });
        if (!challenge.userId) {
          setError('Unable to complete login.');
          return;
        }
        setSession(response.tokens, challenge.userId, challenge.roleCode, email);
        navigate(from, { replace: true });
        return;
      }

      const response = await login({ email, password, deviceId: getDeviceId() });
      if (response.requiresTwoFactor) {
        setChallenge(response);
        return;
      }

      if (response.tokens) {
        setSession(response.tokens, response.userId, response.roleCode, email);
        navigate(from, { replace: true });
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Login failed.');
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-primary px-4">
      <div className="w-full max-w-sm rounded-lg bg-white p-8 shadow-xl">
        <h1 className="mb-1 text-2xl font-bold text-slate-800">Karry Platform</h1>
        <p className="mb-6 text-sm text-slate-500">
          {challenge ? 'Enter your two-factor code' : 'Sign in to continue'}
        </p>

        <form onSubmit={handleLogin} className="space-y-4" aria-busy={loading}>
          {!challenge ? (
            <>
              <label className="block">
                <span className="mb-1 block text-sm text-slate-600">Email</span>
                <input
                  type="email"
                  required
                  autoComplete="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  className="w-full rounded border border-slate-300 px-3 py-2"
                />
              </label>

              <label className="block">
                <span className="mb-1 block text-sm text-slate-600">Password</span>
                <input
                  type="password"
                  required
                  autoComplete="current-password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  className="w-full rounded border border-slate-300 px-3 py-2"
                />
              </label>
            </>
          ) : (
            <>
              <label className="block">
                <span className="mb-1 block text-sm text-slate-600">Authenticator code</span>
                <input
                  type="text"
                  inputMode="numeric"
                  autoComplete="one-time-code"
                  value={twoFactorCode}
                  onChange={(e) => setTwoFactorCode(e.target.value)}
                  className="w-full rounded border border-slate-300 px-3 py-2"
                />
              </label>

              <button
                type="button"
                onClick={() => setChallenge(null)}
                className="text-sm text-accent hover:underline"
              >
                Back to sign in
              </button>
            </>
          )}

          {error && <p className="text-sm text-red-600">{error}</p>}

          <button
            type="submit"
            disabled={loading}
            className="w-full rounded bg-accent px-4 py-2 font-medium text-white transition hover:bg-primary disabled:opacity-50"
          >
            {loading ? 'Please wait…' : challenge ? 'Verify' : 'Sign in'}
          </button>
        </form>
      </div>
    </div>
  );
}
