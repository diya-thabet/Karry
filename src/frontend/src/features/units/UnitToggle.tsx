import { useState } from 'react';
import { convertMeasure, type ConvertMeasureResponse } from '@/lib/api';

const UNITS = ['m3', 't', 'st'];

export function UnitToggle() {
  const [value, setValue] = useState('10');
  const [fromUnit, setFromUnit] = useState('m3');
  const [density, setDensity] = useState('2.65');
  const [moisture, setMoisture] = useState('1.1');
  const [result, setResult] = useState<ConvertMeasureResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function handleConvert() {
    setLoading(true);
    setError(null);
    try {
      const response = await convertMeasure({
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
    <div className="rounded-lg border border-slate-200 bg-white p-6 shadow-sm">
      <h3 className="mb-4 text-lg font-semibold text-slate-800">Dynamic Unit Toggle</h3>

      <div className="grid grid-cols-2 gap-4">
        <label className="block">
          <span className="mb-1 block text-sm text-slate-600">Quantity</span>
          <input
            type="number"
            value={value}
            onChange={(e) => setValue(e.target.value)}
            className="w-full rounded border border-slate-300 px-3 py-2"
          />
        </label>

        <label className="block">
          <span className="mb-1 block text-sm text-slate-600">Unit</span>
          <select
            value={fromUnit}
            onChange={(e) => setFromUnit(e.target.value)}
            className="w-full rounded border border-slate-300 px-3 py-2"
          >
            {UNITS.map((u) => (
              <option key={u} value={u}>
                {u}
              </option>
            ))}
          </select>
        </label>

        <label className="block">
          <span className="mb-1 block text-sm text-slate-600">Density (t/m³)</span>
          <input
            type="number"
            step="0.01"
            value={density}
            onChange={(e) => setDensity(e.target.value)}
            className="w-full rounded border border-slate-300 px-3 py-2"
          />
        </label>

        <label className="block">
          <span className="mb-1 block text-sm text-slate-600">Moisture factor κ</span>
          <input
            type="number"
            step="0.05"
            value={moisture}
            onChange={(e) => setMoisture(e.target.value)}
            className="w-full rounded border border-slate-300 px-3 py-2"
          />
        </label>
      </div>

      <button
        onClick={handleConvert}
        disabled={loading}
        className="mt-6 rounded bg-accent px-4 py-2 font-medium text-white transition hover:bg-primary disabled:opacity-50"
      >
        {loading ? 'Converting…' : 'Convert'}
      </button>

      {error && <p className="mt-4 text-sm text-red-600">{error}</p>}

      {result && (
        <p className="mt-4 text-sm text-slate-700">
          Result:{' '}
          <strong>
            {result.value.toFixed(2)} {result.toUnit}
          </strong>
        </p>
      )}
    </div>
  );
}
