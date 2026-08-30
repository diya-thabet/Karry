import { describe, expect, it } from 'vitest';
import { toPreferenceRequest } from './preferences';

describe('toPreferenceRequest', () => {
  it('passes through valid metric-ton preference', () => {
    expect(toPreferenceRequest({ massUnit: 't', volumeUnit: 'm3' })).toEqual({
      massUnit: 't',
      volumeUnit: 'm3',
    });
  });

  it('passes through valid short-ton preference', () => {
    expect(toPreferenceRequest({ massUnit: 'st', volumeUnit: 'm3' })).toEqual({
      massUnit: 'st',
      volumeUnit: 'm3',
    });
  });

  it('rejects unsupported mass unit', () => {
    // Casting through unknown simulates a bad value reaching validation.
    expect(() => toPreferenceRequest({ massUnit: 'kg' as never, volumeUnit: 'm3' })).toThrow(
      'Unsupported mass unit.',
    );
  });

  it('rejects unsupported volume unit', () => {
    expect(() => toPreferenceRequest({ massUnit: 't', volumeUnit: 'ft3' as never })).toThrow(
      'Unsupported volume unit.',
    );
  });
});
