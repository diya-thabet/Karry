import { useState } from 'react';
import { Button } from '@/components/ui/Button';
import { Card, CardBody, CardHeader } from '@/components/ui/Card';
import { Field } from '@/components/ui/Field';
import { Input } from '@/components/ui/Input';
import { Select } from '@/components/ui/Select';
import { Alert } from '@/components/ui/Alert';
import { getAccessToken } from '@/features/auth/tokenManager';
import { convertMeasure, type ConvertMeasureResponse } from '@/lib/api';

const UNITS = ['m3', 't', 'st'] as const;

export function UnitToggle() {
  const [value, setValue] = useState('10');
  const [fromUnit, setFromUnit] = useState<string>('m3');
  const [density, setDensity] = useState('2.65');
  const [moisture, setMoisture] = useState('1.1');
  const [result, setResult] = useState<ConvertMeasureResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function handleConvert() {
    setLoading(true);
    setError(null);
    try {
      const token = await getAccessToken();
      const response = await convertMeasure(token, {
        value: Number(value),
        fromUnit,
        rhoDryTonPerM3: Number(density),
        kappaMoisture: Number(moisture),
      });
      setResult(response);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown error');
    } finally {
      setLoading(false);
    }
  }

  return (
    <Card>
      <CardHeader>
        <div>
          <h3 className="text-lg font-semibold text-ink">Dynamic Unit Toggle</h3>
          <p className="text-sm text-ink-muted">M = V × ρ × κ_moisture</p>
        </div>
      </CardHeader>
      <CardBody>
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <Field label="Quantity" htmlFor="qty" required>
            <Input
              id="qty"
              type="number"
              value={value}
              onChange={(e) => setValue(e.target.value)}
            />
          </Field>

          <Field label="Unit" htmlFor="unit" required>
            <Select id="unit" value={fromUnit} onChange={(e) => setFromUnit(e.target.value)}>
              {UNITS.map((u) => (
                <option key={u} value={u}>
                  {u === 'm3'
                    ? 'Cubic metres (m³)'
                    : u === 't'
                      ? 'Metric tons (t)'
                      : 'Short tons (st)'}
                </option>
              ))}
            </Select>
          </Field>

          <Field label="Density (t/m³)" htmlFor="density" required>
            <Input
              id="density"
              type="number"
              step="0.01"
              value={density}
              onChange={(e) => setDensity(e.target.value)}
            />
          </Field>

          <Field label="Moisture factor κ" htmlFor="kappa" hint="Clamped to ≥ 1.0">
            <Input
              id="kappa"
              type="number"
              step="0.05"
              value={moisture}
              onChange={(e) => setMoisture(e.target.value)}
            />
          </Field>
        </div>

        <div className="mt-5 flex flex-wrap items-center gap-4">
          <Button onClick={handleConvert} loading={loading}>
            Convert
          </Button>

          {result && (
            <Alert tone="success" className="flex-1 sm:flex-none">
              Result:{' '}
              <strong>
                {result.value.toFixed(2)} {result.toUnit}
              </strong>
            </Alert>
          )}

          {error && (
            <Alert tone="error" className="flex-1 sm:flex-none">
              {error}
            </Alert>
          )}
        </div>
      </CardBody>
    </Card>
  );
}
