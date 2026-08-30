import { FormEvent, useState } from 'react';
import { useAuth } from '@/features/auth/useAuth';
import { useAuthStore } from '@/features/auth/authStore';
import { enableTwoFactor, verifyTwoFactor, disableTwoFactor, getCurrentSession } from '@/lib/api';
import { PageHeader } from '@/components/ui/PageHeader';
import { Card, CardBody, CardHeader } from '@/components/ui/Card';
import { Badge } from '@/components/ui/Badge';
import { Button } from '@/components/ui/Button';
import { Field } from '@/components/ui/Field';
import { Input } from '@/components/ui/Input';
import { Alert } from '@/components/ui/Alert';
import { cn } from '@/lib/cn';

export function SecurityPage() {
  const session = useAuth();
  const accessToken = useAuthStore((s) => s.accessToken);
  const setCurrentSession = useAuthStore((s) => s.setCurrentSession);
  const refreshSession = session.refreshSession;

  const [enroll, setEnroll] = useState<{ secret: string; provisioningUri: string } | null>(null);
  const [verifyCode, setVerifyCode] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState<null | 'enable' | 'verify' | 'disable'>(null);

  async function handleEnable() {
    if (!accessToken) return;
    setError(null);
    setBusy('enable');
    try {
      const response = await enableTwoFactor(accessToken);
      setEnroll(response);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not start 2FA setup.');
    } finally {
      setBusy(null);
    }
  }

  async function handleVerify(event: FormEvent) {
    event.preventDefault();
    if (!accessToken || !enroll) return;
    setError(null);
    setBusy('verify');
    try {
      await verifyTwoFactor(accessToken, enroll.secret, verifyCode.trim());
      setEnroll(null);
      setVerifyCode('');
      await refreshSession();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Invalid code.');
    } finally {
      setBusy(null);
    }
  }

  async function handleDisable() {
    if (!accessToken) return;
    setError(null);
    setBusy('disable');
    try {
      await disableTwoFactor(accessToken);
      await refreshSession();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not disable 2FA.');
    } finally {
      setBusy(null);
    }
  }

  async function refresh() {
    if (!accessToken) return;
    setError(null);
    try {
      const s = await getCurrentSession(accessToken);
      setCurrentSession(s);
    } catch {
      // swallowed: session refresh failures surface on next navigation
    }
  }

  return (
    <div className="space-y-6">
      <PageHeader
        title="Security"
        description="Manage two-factor authentication and review your session."
      />

      <Card>
        <CardHeader>
          <div>
            <h3 className="text-lg font-semibold text-ink">Two-factor authentication</h3>
            <p className="mt-0.5 text-sm text-ink-muted">
              Require an authenticator code in addition to your password.
            </p>
          </div>
          <Badge tone={session.twoFactorEnabled ? 'success' : 'neutral'}>
            {session.twoFactorEnabled ? 'Enabled' : 'Disabled'}
          </Badge>
        </CardHeader>
        <CardBody>
          {error && (
            <Alert tone="error" className="mb-4">
              {error}
            </Alert>
          )}

          {enroll ? (
            <div className="space-y-4">
              <Alert tone="info">
                Scan the URI with your authenticator app (or add the secret manually), then enter a
                6-digit code to confirm.
              </Alert>

              <Field
                label="Setup URI (otpauth://)"
                htmlFor="otpauth"
                hint="Add this to Google Authenticator, Authy, 1Password, etc."
              >
                <Input
                  id="otpauth"
                  readOnly
                  value={enroll.provisioningUri}
                  className="font-mono text-xs"
                />
              </Field>

              <Field label="Secret (base32)" htmlFor="secret">
                <Input id="secret" readOnly value={enroll.secret} className="font-mono" />
              </Field>

              <form onSubmit={handleVerify} className="flex max-w-xs items-end gap-3">
                <Field label="Verification code" htmlFor="verify-code" className="flex-1">
                  <Input
                    id="verify-code"
                    inputMode="numeric"
                    value={verifyCode}
                    onChange={(e) => setVerifyCode(e.target.value)}
                    required
                  />
                </Field>
                <Button type="submit" loading={busy === 'verify'}>
                  Verify
                </Button>
              </form>
            </div>
          ) : session.twoFactorEnabled ? (
            <div className="flex items-center justify-between gap-4">
              <p className="text-sm text-ink-muted">
                Two-factor authentication is protecting this account.
              </p>
              <Button variant="danger" onClick={handleDisable} loading={busy === 'disable'}>
                Disable 2FA
              </Button>
            </div>
          ) : (
            <Button onClick={handleEnable} loading={busy === 'enable'}>
              Enable 2FA
            </Button>
          )}
        </CardBody>
      </Card>

      <Card>
        <CardHeader>
          <div>
            <h3 className="text-lg font-semibold text-ink">Session</h3>
            <p className="mt-0.5 text-sm text-ink-muted">
              Details for the current authenticated session.
            </p>
          </div>
          <Button variant="outline" onClick={() => void refresh()}>
            Refresh
          </Button>
        </CardHeader>
        <CardBody>
          <SessionRow label="User" value={`${session.name ?? '—'} <${session.email ?? '—'}>`} />
          <SessionRow
            label="Tenant"
            value={session.tenantId ?? 'Platform (no tenant)'}
            mono={Boolean(session.tenantId)}
          />
          <SessionRow
            label="Role"
            value={session.isPlatformAdmin ? 'Platform Admin' : (session.roleCode ?? '—')}
          />
          <SessionRow
            label="Permissions"
            value={`${session.permissions.length} permission claim(s)`}
          />
        </CardBody>
      </Card>
    </div>
  );
}

function SessionRow({
  label,
  value,
  mono = false,
}: {
  label: string;
  value: string;
  mono?: boolean;
}) {
  return (
    <div className="flex items-center justify-between gap-4 border-b border-ink/5 py-2.5 last:border-0">
      <span className="text-sm text-ink-muted">{label}</span>
      <span className={cn('text-sm font-medium text-ink', mono && 'font-mono text-xs')}>
        {value}
      </span>
    </div>
  );
}
