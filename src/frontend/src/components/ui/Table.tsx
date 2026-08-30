import { ReactNode } from 'react';
import { cn } from '@/lib/cn';

export function Table({
  columns,
  children,
  className,
}: {
  columns: ReactNode[];
  children: ReactNode;
  className?: string;
}) {
  return (
    <div className={cn('overflow-x-auto', className)}>
      <table className="w-full text-left text-sm">
        <thead>
          <tr className="border-b border-ink/10 text-xs uppercase tracking-wide text-ink-faint">
            {columns.map((column, i) => (
              <th key={i} className="px-6 py-3 font-medium">
                {column}
              </th>
            ))}
          </tr>
        </thead>
        <tbody className="divide-y divide-ink/5">{children}</tbody>
      </table>
    </div>
  );
}
