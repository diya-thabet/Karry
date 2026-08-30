import { SelectHTMLAttributes, forwardRef } from 'react';
import { cn } from '@/lib/cn';

export interface SelectProps extends SelectHTMLAttributes<HTMLSelectElement> {
  invalid?: boolean;
}

const BASE =
  'focus-ring block w-full rounded-lg border border-ink/15 bg-white px-3 py-2 text-sm text-ink';

export const Select = forwardRef<HTMLSelectElement, SelectProps>(function Select(
  { className, invalid = false, children, ...props },
  ref,
) {
  return (
    <select
      ref={ref}
      className={cn(BASE, invalid ? 'border-danger' : 'focus-visible:ring-accent', className)}
      aria-invalid={invalid || undefined}
      {...props}
    >
      {children}
    </select>
  );
});
