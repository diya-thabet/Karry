import { FormEvent, useState } from 'react';
import { useAuthStore } from '@/features/auth/authStore';
import { useAsync } from '@/hooks/useAsync';
import { listUsers, createUser, listRoles } from '@/lib/api';
import type { Role, User } from '@/features/auth/types';
import { PageHeader } from '@/components/ui/PageHeader';
import { Card, CardBody } from '@/components/ui/Card';
import { Table } from '@/components/ui/Table';
import { Button } from '@/components/ui/Button';
import { Avatar } from '@/components/ui/Avatar';
import { Badge } from '@/components/ui/Badge';
import { Field } from '@/components/ui/Field';
import { Input } from '@/components/ui/Input';
import { Select } from '@/components/ui/Select';
import { Modal } from '@/components/ui/Modal';
import { Alert } from '@/components/ui/Alert';
import { Spinner } from '@/components/ui/Spinner';
import { EmptyState } from '@/components/ui/EmptyState';
import { formatDate } from '@/lib/format';

export function UsersPage() {
  const accessToken = useAuthStore((s) => s.accessToken);
  const users = useAsync(
    () => (accessToken ? listUsers(accessToken) : Promise.resolve<User[]>([])),
    [accessToken],
  );
  const roles = useAsync(
    () => (accessToken ? listRoles(accessToken) : Promise.resolve<Role[]>([])),
    [accessToken],
  );

  const [createOpen, setCreateOpen] = useState(false);

  return (
    <div>
      <PageHeader
        title="Users"
        description="Manage users in your organisation and assign roles."
        actions={<Button onClick={() => setCreateOpen(true)}>New user</Button>}
      />

      <Card>
        <CardBody className="px-0 py-0">
          {users.loading ? (
            <div className="flex justify-center px-6 py-16">
              <Spinner className="h-6 w-6 text-accent" />
            </div>
          ) : users.error ? (
            <div className="px-6 py-10">
              <Alert tone="error">{users.error}</Alert>
            </div>
          ) : users.data && users.data.length === 0 ? (
            <EmptyState
              title="No users yet"
              description="Create your first user to start assigning roles."
              action={<Button onClick={() => setCreateOpen(true)}>New user</Button>}
            />
          ) : (
            <Table columns={['User', 'Role', 'Status', '2FA', 'Created']}>
              {(users.data ?? []).map((user) => (
                <tr key={user.userId} className="transition-colors hover:bg-surface">
                  <td className="px-6 py-3">
                    <div className="flex items-center gap-3">
                      <Avatar name={user.name} />
                      <div className="min-w-0 leading-tight">
                        <p className="font-medium text-ink">{user.name}</p>
                        <p className="text-xs text-ink-muted">{user.email}</p>
                      </div>
                    </div>
                  </td>
                  <td className="px-6 py-3">
                    <Badge tone="info">{user.roleCode ?? '—'}</Badge>
                  </td>
                  <td className="px-6 py-3">
                    {user.isActive ? (
                      <Badge tone="success">Active</Badge>
                    ) : (
                      <Badge tone="neutral">Inactive</Badge>
                    )}
                  </td>
                  <td className="px-6 py-3">
                    {user.twoFactorEnabled ? (
                      <Badge tone="success">On</Badge>
                    ) : (
                      <Badge tone="neutral">Off</Badge>
                    )}
                  </td>
                  <td className="px-6 py-3 text-ink-muted">{formatDate(user.createdAtUtc)}</td>
                </tr>
              ))}
            </Table>
          )}
        </CardBody>
      </Card>

      <CreateUserModal
        open={createOpen}
        onClose={() => setCreateOpen(false)}
        accessToken={accessToken}
        roles={roles.data ?? []}
        onCreated={() => void users.reload()}
      />
    </div>
  );
}

function CreateUserModal({
  open,
  onClose,
  accessToken,
  roles,
  onCreated,
}: {
  open: boolean;
  onClose: () => void;
  accessToken: string | null;
  roles: Role[];
  onCreated: () => void;
}) {
  const [email, setEmail] = useState('');
  const [name, setName] = useState('');
  const [password, setPassword] = useState('');
  const [roleId, setRoleId] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (!accessToken || !roleId) return;
    setError(null);
    setSubmitting(true);
    try {
      await createUser(accessToken, { email, name, password, roleId });
      setEmail('');
      setName('');
      setPassword('');
      setRoleId('');
      onClose();
      onCreated();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not create user.');
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="New user"
      description="Create a user and assign a role."
      footer={
        <>
          <Button variant="ghost" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" form="create-user-form" loading={submitting}>
            Create user
          </Button>
        </>
      }
    >
      <form id="create-user-form" onSubmit={handleSubmit} className="space-y-4">
        {error && <Alert tone="error">{error}</Alert>}

        <Field label="Full name" htmlFor="user-name" required>
          <Input id="user-name" value={name} onChange={(e) => setName(e.target.value)} required />
        </Field>

        <Field label="Email" htmlFor="user-email" required>
          <Input
            id="user-email"
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
          />
        </Field>

        <Field
          label="Temporary password"
          htmlFor="user-password"
          required
          hint="Should meet the password policy."
        >
          <Input
            id="user-password"
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
          />
        </Field>

        <Field label="Role" htmlFor="user-role" required>
          <Select
            id="user-role"
            value={roleId}
            onChange={(e) => setRoleId(e.target.value)}
            required
          >
            <option value="" disabled>
              Select a role…
            </option>
            {roles.map((role) => (
              <option key={role.roleId} value={role.roleId}>
                {role.name} ({role.code})
              </option>
            ))}
          </Select>
        </Field>
      </form>
    </Modal>
  );
}
