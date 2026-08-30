import { ReactNode } from 'react';
import { cn } from '@/lib/cn';

type Tone = 'info' | 'error' | 'warning' | 'success';

const TONES: Record<Tone, string> = {
  info: 'border-accent/30 bg-accent/5 text-accent-800',
  error: 'border-danger/30 bg-danger/5 text-danger-700',
  warning: 'border-warning/40 bg-warning/5 text-warning-700',
  success: 'border-success/30 bg-success/5 text-success-700',
};

export function Alert({
  tone = 'info',
  title,
  children,
  className,
}: {
  tone?: Tone;
  title?: string;
  children?: ReactNode;
  className?: string;
}) {
  return (
    <div
      role={tone === 'error' ? 'alert' : 'status'}
      className={cn('rounded-lg border px-4 py-3 text-sm', TONES[tone], className)}
    >
      {title && <p className="font-medium">{title}</p>}
      {children && <div className={cn(title && 'mt-1')}>{children}</div>}
    </div>
  );
}
