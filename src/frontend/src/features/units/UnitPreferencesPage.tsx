import { FormEvent, useEffect, useState } from 'react';
import { useAuth } from '@/features/auth/useAuth';
import { getAccessToken } from '@/features/auth/tokenManager';
import { setUnitPreferences } from '@/lib/api';
import { PageHeader } from '@/components/ui/PageHeader';
import { Card, CardBody } from '@/components/ui/Card';
import { Field } from '@/components/ui/Field';
import { Select } from '@/components/ui/Select';
import { Button } from '@/components/ui/Button';
import { Alert } from '@/components/ui/Alert';
import { UnitToggle } from './UnitToggle';
import { toPreferenceRequest, type MassPreference } from './preferences';

export function UnitPreferencesPage() {
  const { tenantId } = useAuth();
  const [massUnit, setMassUnit] = useState<MassPreference>('t');
  const [status, setStatus] = useState<'idle' | 'saving' | 'saved' | 'error'>('idle');
  const [error, setError] = useState<string | null>(null);

  useEffect(() => setStatus('idle'), [massUnit]);

  async function handleSave(event: FormEvent) {
    event.preventDefault();
    if (!tenantId) return;
    setStatus('saving');
    setError(null);
    try {
      const accessToken = await getAccessToken();
      if (!accessToken) throw new Error('Not authenticated.');
      await setUnitPreferences(accessToken, toPreferenceRequest({ massUnit, volumeUnit: 'm3' }));
      setStatus('saved');
    } catch (err) {
      setStatus('error');
      setError(err instanceof Error ? err.message : 'Could not save preferences.');
    }
  }

  return (
    <div className="space-y-6">
      <PageHeader
        title="Unit Preferences"
        description="Choose your preferred display units. Volume is fixed to cubic metres; mass can be metric or short tons."
      />

      <Card>
        <CardBody>
          <form onSubmit={handleSave} className="max-w-sm space-y-5">
            <Field
              label="Mass unit"
              htmlFor="mass-unit"
              hint="Metric tons (t) or short tons (st) for gravimetric quantities."
            >
              <Select
                id="mass-unit"
                value={massUnit}
                onChange={(e) => setMassUnit(e.target.value as MassPreference)}
              >
                <option value="t">Metric tons (t)</option>
                <option value="st">Short tons (st)</option>
              </Select>
            </Field>

            {status === 'saved' && <Alert tone="success">Preferences saved.</Alert>}
            {status === 'error' && <Alert tone="error">{error}</Alert>}

            <Button type="submit" loading={status === 'saving'}>
              Save preferences
            </Button>
          </form>
        </CardBody>
      </Card>

      <UnitToggle />
    </div>
  );
}
