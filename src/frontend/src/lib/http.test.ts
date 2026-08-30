import { describe, expect, it } from 'vitest';
import { generateIdempotencyKey, parseProblem } from './http';

describe('generateIdempotencyKey', () => {
  it('prepends the provided prefix', () => {
    expect(generateIdempotencyKey('login:user@example.com')).toMatch(/^login:user@example\.com:/);
  });

  it('produces distinct keys for repeated calls', () => {
    const a = generateIdempotencyKey();
    const b = generateIdempotencyKey();
    expect(a).not.toBe(b);
  });
});

describe('parseProblem', () => {
  it('extracts title, detail and code from a problem payload', () => {
    const parsed = parseProblem({
      title: 'Conflict',
      detail: 'Email already exists',
      code: 'EMAIL_EXISTS',
    });
    expect(parsed).toEqual({
      title: 'Conflict',
      detail: 'Email already exists',
      code: 'EMAIL_EXISTS',
    });
  });

  it('returns nulls for non-object input', () => {
    expect(parseProblem('just text')).toEqual({ title: null, detail: null, code: null });
    expect(parseProblem(null)).toEqual({ title: null, detail: null, code: null });
  });
});
