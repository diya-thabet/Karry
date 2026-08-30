import { ReactNode } from 'react';
import { cn } from '@/lib/cn';

type Tone = 'neutral' | 'success' | 'danger' | 'warning' | 'info';

const TONES: Record<Tone, string> = {
  neutral: 'bg-ink/5 text-ink-muted',
  success: 'bg-success/10 text-success-600',
  danger: 'bg-danger/10 text-danger-600',
  warning: 'bg-warning/10 text-warning-600',
  info: 'bg-accent/10 text-accent-700',
};

export function Badge({
  tone = 'neutral',
  className,
  children,
}: {
  tone?: Tone;
  className?: string;
  children: ReactNode;
}) {
  return (
    <span
      className={cn(
        'inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-xs font-medium',
        TONES[tone],
        className,
      )}
    >
      {children}
    </span>
  );
}
