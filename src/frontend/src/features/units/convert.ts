export interface UnitConversionInput {
  value: number;
  fromUnit: 'm3' | 't' | 'st';
  rhoDryTonPerM3: number;
  kappaMoisture: number;
}

export interface UnitConversionResult {
  value: number;
  toUnit: 'm3' | 't' | 'st';
}

const SHORT_TON_TO_METRIC_TON = 0.90718474;

function clampMoisture(kappa: number): number {
  return kappa < 1.0 ? 1.0 : kappa;
}

/**
 * Mirrors the backend conversion using M = V × ρ × κ_moisture.
 * Pure function kept dependency-free so it is trivially unit-testable.
 */
export function convertUnits(input: UnitConversionInput): UnitConversionResult {
  if (input.value <= 0 || input.rhoDryTonPerM3 <= 0) {
    throw new Error('Value and density must be positive.');
  }

  const kappa = clampMoisture(input.kappaMoisture);

  if (input.fromUnit === 'm3') {
    const metricTons = input.value * input.rhoDryTonPerM3 * kappa;
    return { value: metricTons, toUnit: 't' };
  }

  const metricTons = input.fromUnit === 't' ? input.value : input.value * SHORT_TON_TO_METRIC_TON;

  const volume = metricTons / (input.rhoDryTonPerM3 * kappa);
  return { value: volume, toUnit: 'm3' };
}
