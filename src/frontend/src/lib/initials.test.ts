import { describe, expect, it } from 'vitest';
import { initials } from './initials';

describe('initials', () => {
  it('uses first and last name', () => {
    expect(initials('Olaf the Operator')).toBe('OO');
  });

  it('handles a single word', () => {
    expect(initials('Karry')).toBe('K');
  });

  it('falls back for empty input', () => {
    expect(initials('   ')).toBe('?');
    expect(initials('')).toBe('?');
  });
});
