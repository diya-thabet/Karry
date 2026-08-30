import { FormEvent, useState } from 'react';
import { useAuthStore } from '@/features/auth/authStore';
import { useAuth } from '@/features/auth/useAuth';
import { createTenant } from '@/lib/api';
import { PageHeader } from '@/components/ui/PageHeader';
import { Card, CardBody } from '@/components/ui/Card';
import { Field } from '@/components/ui/Field';
import { Input } from '@/components/ui/Input';
import { Select } from '@/components/ui/Select';
import { Button } from '@/components/ui/Button';
import { Alert } from '@/components/ui/Alert';
import { Badge } from '@/components/ui/Badge';

const CURRENCIES = ['USD', 'EUR', 'KES', 'ZAR', 'NGN', 'GBP'];

const COUNTRIES = [
  { code: 'KE', label: 'Kenya' },
  { code: 'ZA', label: 'South Africa' },
  { code: 'NG', label: 'Nigeria' },
  { code: 'GH', label: 'Ghana' },
  { code: 'TZ', label: 'Tanzania' },
  { code: 'ET', label: 'Ethiopia' },
  { code: 'UG', label: 'Uganda' },
  { code: 'US', label: 'United States' },
  { code: 'GB', label: 'United Kingdom' },
];

export function TenantsPage() {
  const accessToken = useAuthStore((s) => s.accessToken);
  const { isPlatformAdmin } = useAuth();

  const [name, setName] = useState('');
  const [country, setCountry] = useState('KE');
  const [currency, setCurrency] = useState('USD');
  const [timezone, setTimezone] = useState('UTC');
  const [locale, setLocale] = useState('en');
  const [adminEmail, setAdminEmail] = useState('');
  const [adminPassword, setAdminPassword] = useState('');
  const [adminName, setAdminName] = useState('');
  const [result, setResult] = useState<{ tenantId: string; name: string } | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const provideAdmin = Boolean(adminEmail || adminPassword || adminName);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (!accessToken) return;
    setError(null);
    setResult(null);
    setSubmitting(true);
    try {
      const tenant = await createTenant(accessToken, {
        name,
        country,
        currency,
        timezone,
        locale,
        adminEmail: provideAdmin ? adminEmail : undefined,
        adminPassword: provideAdmin ? adminPassword : undefined,
        adminName: provideAdmin ? adminName : undefined,
      });
      setResult(tenant);
      setName('');
      setAdminEmail('');
      setAdminPassword('');
      setAdminName('');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not create tenant.');
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="max-w-2xl">
      <PageHeader
        title="Provision a Tenant"
        description="Create a new quarries tenant, its six system roles, default unit preferences, and an optional admin user."
        actions={isPlatformAdmin ? <Badge tone="info">Platform admin</Badge> : undefined}
      />

      <Card>
        <CardBody>
          {!isPlatformAdmin ? (
            <Alert tone="error">Only platform administrators can provision tenants.</Alert>
          ) : (
            <form onSubmit={handleSubmit} className="space-y-5">
              {error && <Alert tone="error">{error}</Alert>}
              {result && (
                <Alert tone="success">
                  Tenant <strong>{result.name}</strong> created (id: {result.tenantId}).
                </Alert>
              )}

              <Field label="Tenant name" htmlFor="tenant-name" required>
                <Input
                  id="tenant-name"
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  required
                />
              </Field>

              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <Field label="Country" htmlFor="country" required>
                  <Select id="country" value={country} onChange={(e) => setCountry(e.target.value)}>
                    {COUNTRIES.map((c) => (
                      <option key={c.code} value={c.code}>
                        {c.label} ({c.code})
                      </option>
                    ))}
                  </Select>
                </Field>

                <Field label="Currency" htmlFor="currency" required>
                  <Select
                    id="currency"
                    value={currency}
                    onChange={(e) => setCurrency(e.target.value)}
                  >
                    {CURRENCIES.map((c) => (
                      <option key={c} value={c}>
                        {c}
                      </option>
                    ))}
                  </Select>
                </Field>

                <Field label="Timezone" htmlFor="timezone" required>
                  <Select
                    id="timezone"
                    value={timezone}
                    onChange={(e) => setTimezone(e.target.value)}
                  >
                    <option value="UTC">UTC</option>
                    <option value="Africa/Nairobi">Africa/Nairobi (EAT)</option>
                    <option value="Africa/Johannesburg">Africa/Johannesburg (SAST)</option>
                    <option value="Africa/Lagos">Africa/Lagos (WAT)</option>
                    <option value="Africa/Accra">Africa/Accra (GMT)</option>
                  </Select>
                </Field>

                <Field label="Locale" htmlFor="locale" required>
                  <Select id="locale" value={locale} onChange={(e) => setLocale(e.target.value)}>
                    <option value="en">English (en)</option>
                    <option value="sw">Swahili (sw)</option>
                  </Select>
                </Field>
              </div>

              <div className="rounded-xl border border-ink/10 bg-surface p-4">
                <p className="mb-3 text-sm font-medium text-ink">
                  Initial administrator (optional)
                </p>
                <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                  <Field label="Admin name" htmlFor="admin-name">
                    <Input
                      id="admin-name"
                      value={adminName}
                      onChange={(e) => setAdminName(e.target.value)}
                    />
                  </Field>
                  <Field label="Admin email" htmlFor="admin-email">
                    <Input
                      id="admin-email"
                      type="email"
                      value={adminEmail}
                      onChange={(e) => setAdminEmail(e.target.value)}
                    />
                  </Field>
                  <Field label="Admin password" htmlFor="admin-password" className="sm:col-span-2">
                    <Input
                      id="admin-password"
                      type="password"
                      value={adminPassword}
                      onChange={(e) => setAdminPassword(e.target.value)}
                      placeholder="Leave blank to skip admin provisioning"
                    />
                  </Field>
                </div>
              </div>

              <Button type="submit" loading={submitting}>
                Create tenant
              </Button>
            </form>
          )}
        </CardBody>
      </Card>
    </div>
  );
}
