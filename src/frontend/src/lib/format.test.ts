import { describe, expect, it } from 'vitest';
import { formatDate, formatDateTime } from './format';

describe('formatDate', () => {
  it('returns a formatted date for a valid ISO string', () => {
    const result = formatDate('2026-08-30T12:00:00Z');
    expect(result).toContain('Aug');
    expect(result).toContain('30');
    expect(result).toContain('2026');
  });

  it('returns — for null or undefined', () => {
    expect(formatDate(null)).toBe('—');
    expect(formatDate(undefined)).toBe('—');
  });

  it('returns — for invalid date strings', () => {
    expect(formatDate('not-a-date')).toBe('—');
  });
});

describe('formatDateTime', () => {
  it('includes time in the output', () => {
    const result = formatDateTime('2026-08-30T14:30:00Z');
    expect(result).toContain('Aug');
    expect(result).toContain('30');
    expect(result).toContain(':');
  });

  it('returns — for null input', () => {
    expect(formatDateTime(null)).toBe('—');
  });
});
