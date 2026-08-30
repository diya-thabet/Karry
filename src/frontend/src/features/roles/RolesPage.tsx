import { FormEvent, useState } from 'react';
import { useAuthStore } from '@/features/auth/authStore';
import { useAsync } from '@/hooks/useAsync';
import { listRoles, createRole } from '@/lib/api';
import type { Role } from '@/features/auth/types';
import { PageHeader } from '@/components/ui/PageHeader';
import { Card, CardBody } from '@/components/ui/Card';
import { Table } from '@/components/ui/Table';
import { Button } from '@/components/ui/Button';
import { Badge } from '@/components/ui/Badge';
import { Field } from '@/components/ui/Field';
import { Input } from '@/components/ui/Input';
import { Modal } from '@/components/ui/Modal';
import { Alert } from '@/components/ui/Alert';
import { Spinner } from '@/components/ui/Spinner';
import { EmptyState } from '@/components/ui/EmptyState';

export function RolesPage() {
  const accessToken = useAuthStore((s) => s.accessToken);
  const roles = useAsync(
    () => (accessToken ? listRoles(accessToken) : Promise.resolve<Role[]>([])),
    [accessToken],
  );

  const [createOpen, setCreateOpen] = useState(false);

  return (
    <div>
      <PageHeader
        title="Roles"
        description="Define who can do what across the organisation."
        actions={<Button onClick={() => setCreateOpen(true)}>New role</Button>}
      />

      <Card>
        <CardBody className="px-0 py-0">
          {roles.loading ? (
            <div className="flex justify-center px-6 py-16">
              <Spinner className="h-6 w-6 text-accent" />
            </div>
          ) : roles.error ? (
            <div className="px-6 py-10">
              <Alert tone="error">{roles.error}</Alert>
            </div>
          ) : roles.data && roles.data.length === 0 ? (
            <EmptyState
              title="No roles yet"
              description="Create your first role."
              action={<Button onClick={() => setCreateOpen(true)}>New role</Button>}
            />
          ) : (
            <Table columns={['Role', 'Code', 'Permissions']}>
              {(roles.data ?? []).map((role) => (
                <tr key={role.roleId} className="transition-colors hover:bg-surface">
                  <td className="px-6 py-3">
                    <p className="font-medium text-ink">{role.name}</p>
                    {role.description && (
                      <p className="text-xs text-ink-muted">{role.description}</p>
                    )}
                  </td>
                  <td className="px-6 py-3">
                    <code className="rounded bg-ink/5 px-1.5 py-0.5 text-xs text-ink-muted">
                      {role.code}
                    </code>
                  </td>
                  <td className="px-6 py-3">
                    <div className="flex max-w-md flex-wrap gap-1">
                      {role.permissions.length === 0 ? (
                        <span className="text-xs text-ink-faint">No permissions</span>
                      ) : (
                        role.permissions.map((p) => (
                          <Badge key={p} tone="neutral">
                            {p}
                          </Badge>
                        ))
                      )}
                    </div>
                  </td>
                </tr>
              ))}
            </Table>
          )}
        </CardBody>
      </Card>

      <CreateRoleModal
        open={createOpen}
        onClose={() => setCreateOpen(false)}
        accessToken={accessToken}
        onCreated={() => void roles.reload()}
      />
    </div>
  );
}

function CreateRoleModal({
  open,
  onClose,
  accessToken,
  onCreated,
}: {
  open: boolean;
  onClose: () => void;
  accessToken: string | null;
  onCreated: () => void;
}) {
  const [code, setCode] = useState('');
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (!accessToken) return;
    setError(null);
    setSubmitting(true);
    try {
      await createRole(accessToken, {
        code,
        name,
        description: description.trim() ? description : null,
      });
      setCode('');
      setName('');
      setDescription('');
      onClose();
      onCreated();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not create role.');
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="New role"
      description="Roles grant a set of fixed capabilities from the permission catalog."
      footer={
        <>
          <Button variant="ghost" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" form="create-role-form" loading={submitting}>
            Create role
          </Button>
        </>
      }
    >
      <form id="create-role-form" onSubmit={handleSubmit} className="space-y-4">
        {error && <Alert tone="error">{error}</Alert>}

        <Field
          label="Code"
          htmlFor="role-code"
          required
          hint="Lowercase identifier, e.g. supervisor"
        >
          <Input id="role-code" value={code} onChange={(e) => setCode(e.target.value)} required />
        </Field>

        <Field label="Display name" htmlFor="role-name" required>
          <Input id="role-name" value={name} onChange={(e) => setName(e.target.value)} required />
        </Field>

        <Field label="Description" htmlFor="role-description">
          <Input
            id="role-description"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
          />
        </Field>
      </form>
    </Modal>
  );
}
