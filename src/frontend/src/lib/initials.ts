export function initials(name: string): string {
  const parts = name.trim().split(/\s+/).filter(Boolean);
  const start = parts[0]?.charAt(0) ?? '';
  const end = parts.length > 1 ? parts[parts.length - 1].charAt(0) : '';
  return (start + end).toUpperCase() || '?';
}
