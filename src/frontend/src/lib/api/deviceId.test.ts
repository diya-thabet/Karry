import { describe, expect, it } from 'vitest';
import { getDeviceId } from './auth';

describe('getDeviceId', () => {
  it('falls back to "browser" when localStorage is unavailable (Node env)', () => {
    const id = getDeviceId();
    expect(id).toBe('browser');
  });

  it('returns a stable value for the same session', () => {
    const id1 = getDeviceId();
    const id2 = getDeviceId();
    expect(id1).toBe(id2);
  });
});
