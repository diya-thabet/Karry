import { describe, expect, it } from 'vitest';
import { convertUnits } from './convert';

describe('convertUnits', () => {
  it('converts cubic meters to metric tons with density and moisture', () => {
    const result = convertUnits({
      value: 100,
      fromUnit: 'm3',
      rhoDryTonPerM3: 2.65,
      kappaMoisture: 1.1,
    });
    expect(result).toEqual({ value: 100 * 2.65 * 1.1, toUnit: 't' });
  });

  it('converts short tons to volume', () => {
    const result = convertUnits({
      value: 1,
      fromUnit: 'st',
      rhoDryTonPerM3: 2.0,
      kappaMoisture: 1.0,
    });
    expect(result.toUnit).toBe('m3');
    expect(result.value).toBeCloseTo(0.45359237, 6);
  });

  it('clamps moisture factor below 1.0', () => {
    const result = convertUnits({
      value: 10,
      fromUnit: 'm3',
      rhoDryTonPerM3: 2.0,
      kappaMoisture: 0.5,
    });
    expect(result.value).toBe(10 * 2.0 * 1.0);
  });

  it('rejects non-positive values', () => {
    expect(() =>
      convertUnits({ value: -1, fromUnit: 'm3', rhoDryTonPerM3: 2.0, kappaMoisture: 1.0 }),
    ).toThrow();
  });
});
