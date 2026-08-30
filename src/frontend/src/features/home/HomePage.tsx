import { useAuth } from '@/features/auth/useAuth';
import { Card, CardBody } from '@/components/ui/Card';
import { Badge } from '@/components/ui/Badge';
import { Avatar } from '@/components/ui/Avatar';
import { UnitToggle } from '@/features/units/UnitToggle';

export function HomePage() {
  const { name, email, roleCode, isPlatformAdmin, tenantId, permissions } = useAuth();
  const displayName = name ?? email ?? 'there';

  return (
    <div className="space-y-6">
      <Card>
        <CardBody className="flex flex-wrap items-center justify-between gap-4">
          <div className="flex items-center gap-4">
            <Avatar name={displayName} className="h-12 w-12 text-base" />
            <div>
              <h2 className="text-xl font-bold text-ink">
                Welcome back, {displayName.split(' ')[0]}
              </h2>
              <p className="text-sm text-ink-muted">
                {isPlatformAdmin
                  ? 'Platform administration'
                  : `Workspace ${tenantId ? 'tenant' : ''}`}
              </p>
            </div>
          </div>
          <div className="flex items-center gap-2">
            <Badge tone={isPlatformAdmin ? 'info' : 'neutral'}>
              {isPlatformAdmin ? 'Platform Admin' : (roleCode ?? 'Member')}
            </Badge>
            <Badge tone="neutral">{permissions.length} permissions</Badge>
          </div>
        </CardBody>
      </Card>

      <div>
        <h3 className="mb-3 text-sm font-semibold uppercase tracking-wide text-ink-faint">Tools</h3>
        <UnitToggle />
      </div>
    </div>
  );
}
