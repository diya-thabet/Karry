export type ClassValue = string | number | null | undefined | false;

/**
 * Joins class names, filtering falsy values. Kept dependency-free (no clsx/tailwind-merge)
 * while still supporting conditional members and arrays.
 */
export function cn(...values: ClassValue[]): string {
  return values.filter(Boolean).join(' ');
}
