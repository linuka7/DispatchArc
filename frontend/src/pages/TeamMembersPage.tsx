import { useState } from 'react'
import {
  useMutation,
  useQuery,
  useQueryClient,
} from '@tanstack/react-query'
import {
  Mail,
  Plus,
  ShieldCheck,
  UserRound,
  X,
} from 'lucide-react'
import {
  createTechnician,
  getTeamMembers,
} from '../api/technicians'
import type { UserRole } from '../api/types'
import './TeamMembersPage.css'

interface TeamMembersPageProps {
  tenantId: string
}

const roleLabels: Record<UserRole, string> = {
  Owner: 'Owner',
  Dispatcher: 'Dispatcher',
  Technician: 'Technician',
  Finance: 'Finance',
}

function TeamMembersPage({
  tenantId,
}: TeamMembersPageProps) {
  const queryClient = useQueryClient()

  const [createOpen, setCreateOpen] = useState(false)

  const [fullName, setFullName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [role, setRole] =
    useState<'Technician'>('Technician')

  const [error, setError] = useState('')

  const membersQuery = useQuery({
    queryKey: ['team-members', tenantId],
    queryFn: () => getTeamMembers(tenantId),
  })

  const createMutation = useMutation({
    mutationFn: () =>
      createTechnician(tenantId, {
        fullName: fullName.trim(),
        email: email.trim(),
        password,
        role,
      }),

    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: ['team-members', tenantId],
      })

      await queryClient.invalidateQueries({
        queryKey: ['technicians', tenantId],
      })

      setFullName('')
      setEmail('')
      setPassword('')
      setRole('Technician')
      setError('')
      setCreateOpen(false)
    },

    onError: (mutationError) => {
      setError(
        mutationError instanceof Error
          ? mutationError.message
          : 'Unable to create team member.',
      )
    },
  })

  const members = membersQuery.data ?? []

  function submitCreate(
    event: React.FormEvent<HTMLFormElement>,
  ) {
    event.preventDefault()
    setError('')

    if (!fullName.trim()) {
      setError('Full name is required.')
      return
    }

    if (!email.trim()) {
      setError('Email is required.')
      return
    }

    if (password.length < 8) {
      setError(
        'Password must contain at least 8 characters.',
      )
      return
    }

    createMutation.mutate()
  }

  function closeModal() {
    if (createMutation.isPending) {
      return
    }

    setCreateOpen(false)
    setError('')
  }

  return (
    <main className="dashboard team-members-workspace">
      <section className="team-members-hero">
        <div>
          <p className="eyebrow">
            Dispatch workspace
          </p>

          <h1>Team Members</h1>

          <p className="team-members-hero-copy">
            Manage dispatchers, technicians and
            workspace staff.
          </p>
        </div>

        <button
          className="primary-button"
          onClick={() => {
            setError('')
            setCreateOpen(true)
          }}
          type="button"
        >
          <Plus size={17} />
          Add team member
        </button>
      </section>

      <section className="team-members-summary">
        <article>
          <UserRound size={19} />

          <div>
            <span>Total members</span>
            <strong>{members.length}</strong>
          </div>
        </article>

        <article>
          <ShieldCheck size={19} />

          <div>
            <span>Technicians</span>

            <strong>
              {
                members.filter(
                  (member) =>
                    member.role ===
                    'Technician' &&
                    member.isActive,
                ).length
              }
            </strong>
          </div>
        </article>
      </section>

      <section className="team-members-panel">
        <header className="team-members-panel-header">
          <div>
            <p className="eyebrow">
              Workspace access
            </p>

            <h2>Team directory</h2>
          </div>

          <span>
            {membersQuery.isFetching
              ? 'Refreshing...'
              : `${members.length} members`}
          </span>
        </header>

        {membersQuery.isLoading ? (
          <div className="team-members-state">
            <div className="team-members-spinner" />

            <strong>
              Loading team members
            </strong>

            <p>
              Fetching workspace users.
            </p>
          </div>
        ) : membersQuery.isError ? (
          <div className="team-members-state error">
            <strong>
              Could not load team members
            </strong>

            <p>
              Check that the DispatchArc API is
              running and try again.
            </p>

            <button
              className="secondary-button"
              onClick={() =>
                membersQuery.refetch()
              }
              type="button"
            >
              Try again
            </button>
          </div>
        ) : members.length === 0 ? (
          <div className="team-members-state">
            <UserRound size={26} />

            <strong>
              No team members yet
            </strong>

            <p>
              Create your first technician to
              start dispatching jobs.
            </p>

            <button
              className="primary-button"
              onClick={() => {
                setError('')
                setCreateOpen(true)
              }}
              type="button"
            >
              <Plus size={16} />
              Add technician
            </button>
          </div>
        ) : (
          <div className="team-members-list">
            {members.map((member) => (
              <article
                className="team-member-row"
                key={member.id}
              >
                <div className="team-member-avatar">
                  {member.fullName
                    .charAt(0)
                    .toUpperCase()}
                </div>

                <div className="team-member-main">
                  <strong>
                    {member.fullName}
                  </strong>

                  <span>
                    <Mail size={14} />
                    {member.email}
                  </span>
                </div>

                <span
                  className={`team-role role-${member.role.toLowerCase()}`}
                >
                  {roleLabels[member.role]}
                </span>

                <span
                  className={
                    member.isActive
                      ? 'team-status active'
                      : 'team-status'
                  }
                >
                  {member.isActive
                    ? 'Active'
                    : 'Inactive'}
                </span>
              </article>
            ))}
          </div>
        )}
      </section>

      {createOpen && (
        <div className="team-modal-backdrop">
          <section
            aria-modal="true"
            aria-labelledby="create-member-title"
            className="team-modal"
            role="dialog"
          >
            <header className="team-modal-header">
              <div>
                <p className="eyebrow">
                  Workspace setup
                </p>

                <h2 id="create-member-title">
                  Add team member
                </h2>
              </div>

              <button
                aria-label="Close"
                className="job-modal-close"
                onClick={closeModal}
                type="button"
              >
                <X size={18} />
              </button>
            </header>

            <form onSubmit={submitCreate}>
              <label className="team-form-field">
                <span>Full name</span>

                <input
                  onChange={(event) =>
                    setFullName(
                      event.target.value,
                    )
                  }
                  placeholder="e.g. John Smith"
                  required
                  type="text"
                  value={fullName}
                />
              </label>

              <label className="team-form-field">
                <span>Email</span>

                <input
                  onChange={(event) =>
                    setEmail(event.target.value)
                  }
                  placeholder="e.g. john@company.com"
                  required
                  type="email"
                  value={email}
                />
              </label>

              <label className="team-form-field">
                <span>Password</span>

                <input
                  onChange={(event) =>
                    setPassword(
                      event.target.value,
                    )
                  }
                  placeholder="Minimum 8 characters"
                  required
                  type="password"
                  value={password}
                />
              </label>

              <label className="team-form-field">
                <span>Role</span>

                <select
                  onChange={(event) =>
                    setRole(
                      event.target.value as
                        'Technician',
                    )
                  }
                  value={role}
                >
                  <option value="Technician">
                    Technician
                  </option>
                </select>
              </label>

              {error && (
                <div className="team-form-error">
                  {error}
                </div>
              )}

              <footer className="team-modal-actions">
                <button
                  className="secondary-button"
                  onClick={closeModal}
                  type="button"
                >
                  Cancel
                </button>

                <button
                  className="primary-button"
                  disabled={
                    createMutation.isPending
                  }
                  type="submit"
                >
                  <Plus size={16} />

                  {createMutation.isPending
                    ? 'Creating...'
                    : 'Create technician'}
                </button>
              </footer>
            </form>
          </section>
        </div>
      )}
    </main>
  )
}

export default TeamMembersPage