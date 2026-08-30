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

  it('rejects zero value', () => {
    expect(() =>
      convertUnits({ value: 0, fromUnit: 'm3', rhoDryTonPerM3: 2.0, kappaMoisture: 1.0 }),
    ).toThrow();
  });

  it('rejects non-positive density', () => {
    expect(() =>
      convertUnits({ value: 10, fromUnit: 'm3', rhoDryTonPerM3: 0, kappaMoisture: 1.0 }),
    ).toThrow();
  });

  it('round-trips m3 → t → m3', () => {
    const t = convertUnits({
      value: 100,
      fromUnit: 'm3',
      rhoDryTonPerM3: 2.65,
      kappaMoisture: 1.0,
    });
    expect(t.toUnit).toBe('t');
    const back = convertUnits({
      value: t.value,
      fromUnit: 't',
      rhoDryTonPerM3: 2.65,
      kappaMoisture: 1.0,
    });
    expect(back.toUnit).toBe('m3');
    expect(back.value).toBeCloseTo(100, 10);
  });

  it('round-trips st → m3 → t (st converted via t)', () => {
    const m3 = convertUnits({ value: 2, fromUnit: 'st', rhoDryTonPerM3: 2.65, kappaMoisture: 1.0 });
    expect(m3.toUnit).toBe('m3');
    const back = convertUnits({
      value: m3.value,
      fromUnit: 'm3',
      rhoDryTonPerM3: 2.65,
      kappaMoisture: 1.0,
    });
    expect(back.toUnit).toBe('t');
    expect(back.value).toBeCloseTo(2 * 0.90718474, 5);
  });

  it('treats moisture factor at exactly 1.0 as-is', () => {
    const result = convertUnits({
      value: 50,
      fromUnit: 'm3',
      rhoDryTonPerM3: 2.0,
      kappaMoisture: 1.0,
    });
    expect(result.value).toBe(100);
  });

  it('accepts high moisture factors', () => {
    const result = convertUnits({
      value: 10,
      fromUnit: 'm3',
      rhoDryTonPerM3: 2.0,
      kappaMoisture: 2.5,
    });
    expect(result.value).toBe(10 * 2.0 * 2.5);
  });
});
