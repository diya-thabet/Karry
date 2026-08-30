export type MassPreference = 't' | 'st';
export type VolumePreference = 'm3';

export interface UnitPreferencesInput {
  massUnit: MassPreference;
  volumeUnit: VolumePreference;
}

export interface UnitPreferencesPayload {
  massUnit: MassPreference;
  volumeUnit: VolumePreference;
}

const MASS_UNITS = new Set<MassPreference>(['t', 'st']);
const VOLUME_UNITS = new Set<VolumePreference>(['m3']);

/**
 * Pure validation/normalisation of unit preferences before persistence. Kept
 * dependency-free so it is trivially unit-testable.
 */
export function toPreferenceRequest(input: UnitPreferencesInput): UnitPreferencesPayload {
  if (!MASS_UNITS.has(input.massUnit)) {
    throw new Error('Unsupported mass unit.');
  }
  if (!VOLUME_UNITS.has(input.volumeUnit)) {
    throw new Error('Unsupported volume unit.');
  }
  return { massUnit: input.massUnit, volumeUnit: input.volumeUnit };
}
