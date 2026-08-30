import { cn } from '@/lib/cn';
import { initials } from '@/lib/initials';

const PALETTES = [
  'bg-accent/20 text-accent-700',
  'bg-primary/15 text-primary-700',
  'bg-emerald-500/15 text-emerald-700',
  'bg-violet-500/15 text-violet-700',
  'bg-amber-500/15 text-amber-700',
  'bg-rose-500/15 text-rose-700',
];

function hashString(value: string): number {
  let hash = 0;
  for (let i = 0; i < value.length; i += 1) {
    hash = (hash << 5) - hash + value.charCodeAt(i);
    hash |= 0;
  }
  return Math.abs(hash);
}

export function Avatar({ name, className }: { name: string; className?: string }) {
  const palette = PALETTES[hashString(name) % PALETTES.length];
  return (
    <span
      className={cn(
        'inline-flex h-9 w-9 shrink-0 select-none items-center justify-center rounded-full text-sm font-semibold',
        palette,
        className,
      )}
      aria-hidden="true"
    >
      {initials(name)}
    </span>
  );
}
