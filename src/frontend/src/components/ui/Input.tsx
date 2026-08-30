import { InputHTMLAttributes, forwardRef } from 'react';
import { cn } from '@/lib/cn';

export interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  invalid?: boolean;
}

const BASE =
  'focus-ring block w-full rounded-lg border border-ink/15 bg-white px-3 py-2 text-sm text-ink placeholder:text-ink-faint';

export const Input = forwardRef<HTMLInputElement, InputProps>(function Input(
  { className, invalid = false, ...props },
  ref,
) {
  return (
    <input
      ref={ref}
      className={cn(
        BASE,
        invalid ? 'border-danger focus-visible:ring-danger' : 'focus-visible:ring-accent',
        className,
      )}
      aria-invalid={invalid || undefined}
      {...props}
    />
  );
});
