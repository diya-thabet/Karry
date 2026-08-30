import { FormEvent, useState } from 'react';
import { Navigate, useLocation, useNavigate } from 'react-router-dom';
import { Button } from '@/components/ui/Button';
import { Field } from '@/components/ui/Field';
import { Input } from '@/components/ui/Input';
import { Alert } from '@/components/ui/Alert';
import { selectIsAuthenticated, useAuthStore } from './authStore';
import { useAuth } from './useAuth';
import { getDeviceId, login, twoFactorLogin } from '@/lib/api';
import type { LoginResponse } from './types';

export function LoginPage() {
  const isAuthenticated = useAuthStore(selectIsAuthenticated);
  const setTokens = useAuthStore((s) => s.setTokens);
  const refreshSession = useAuth().refreshSession;
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

  async function completeAuth(response: LoginResponse) {
    if (!response.tokens) return;
    setTokens(response.tokens, {
      userId: response.userId,
      roleCode: response.roleCode,
      email,
    });
    try {
      await refreshSession();
    } finally {
      navigate(from, { replace: true });
    }
  }

  async function handleLogin(event: FormEvent) {
    event.preventDefault();
    setError(null);
    setLoading(true);

    try {
      if (challenge) {
        const response = await twoFactorLogin({
          email,
          code: twoFactorCode.trim(),
          deviceId: getDeviceId(),
        });
        await completeAuth(response);
        return;
      }

      const response = await login({ email, password, deviceId: getDeviceId() });
      if (response.requiresTwoFactor && !response.tokens) {
        setChallenge(response);
        return;
      }
      await completeAuth(response);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Login failed.');
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-primary px-4">
      <div className="w-full max-w-sm">
        <div className="mb-6 text-center">
          <div className="mx-auto mb-3 flex h-14 w-14 items-center justify-center rounded-2xl bg-white/10 text-2xl font-black text-white">
            K
          </div>
          <h1 className="text-2xl font-bold text-white">Karry Platform</h1>
          <p className="mt-1 text-sm text-white/70">Enterprise quarry &amp; mining management</p>
        </div>

        <div className="rounded-2xl bg-white p-8 shadow-card">
          <h2 className="mb-1 text-lg font-semibold text-ink">
            {challenge ? 'Two-factor verification' : 'Sign in to your account'}
          </h2>
          <p className="mb-6 text-sm text-ink-muted">
            {challenge
              ? 'Enter the code from your authenticator app to continue.'
              : 'Enter your credentials to access the platform.'}
          </p>

          {error && (
            <Alert tone="error" className="mb-4">
              {error}
            </Alert>
          )}

          <form onSubmit={handleLogin} className="space-y-4" aria-busy={loading}>
            {!challenge ? (
              <>
                <Field label="Email" htmlFor="email" required>
                  <Input
                    id="email"
                    type="email"
                    required
                    autoComplete="email"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                  />
                </Field>

                <Field label="Password" htmlFor="password" required>
                  <Input
                    id="password"
                    type="password"
                    required
                    autoComplete="current-password"
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                  />
                </Field>
              </>
            ) : (
              <>
                <Field
                  label="Authenticator code"
                  htmlFor="2fa"
                  required
                  hint="6-digit code from your app"
                >
                  <Input
                    id="2fa"
                    type="text"
                    inputMode="numeric"
                    autoComplete="one-time-code"
                    value={twoFactorCode}
                    onChange={(e) => setTwoFactorCode(e.target.value)}
                  />
                </Field>

                <button
                  type="button"
                  onClick={() => {
                    setChallenge(null);
                    setTwoFactorCode('');
                  }}
                  className="text-sm text-accent hover:underline"
                >
                  Back to sign in
                </button>
              </>
            )}

            <Button type="submit" fullWidth loading={loading}>
              {challenge ? 'Verify' : 'Sign in'}
            </Button>
          </form>
        </div>
      </div>
    </div>
  );
}
