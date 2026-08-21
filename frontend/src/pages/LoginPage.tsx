import { useState } from 'react'
import type { FormEvent } from 'react'
import {
  ArrowRight,
  Building2,
  Eye,
  EyeOff,
  LockKeyhole,
  ShieldCheck,
} from 'lucide-react'
import { useNavigate } from 'react-router-dom'
import { ApiError } from '../api/client'
import { login } from '../api/auth'
import './LoginPage.css'

const workspaceStorageKey = 'dispatcharc.workspace'

export default function LoginPage() {
  const navigate = useNavigate()

  const rememberedWorkspace =
    localStorage.getItem(workspaceStorageKey) ?? ''

  const [tenantId, setTenantId] = useState(rememberedWorkspace)
  const [workspaceRemembered, setWorkspaceRemembered] = useState(
    Boolean(rememberedWorkspace),
  )
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [error, setError] = useState('')
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    const normalizedTenantId = tenantId.trim()

    setError('')
    setSubmitting(true)

    try {
      await login({
        tenantId: normalizedTenantId,
        email: email.trim(),
        password,
      })

      localStorage.setItem(
        workspaceStorageKey,
        normalizedTenantId,
      )

      navigate('/dashboard', { replace: true })
    } catch (exception) {
      if (exception instanceof ApiError) {
        setError(exception.message)
      } else {
        setError('Unable to connect to DispatchArc API.')
      }
    } finally {
      setSubmitting(false)
    }
  }

  function changeWorkspace() {
    localStorage.removeItem(workspaceStorageKey)
    setTenantId('')
    setWorkspaceRemembered(false)
    setError('')
  }

  return (
    <main className="login-page">
      <section className="login-brand-panel">
        <div className="login-brand">
          <div className="login-brand-mark">
            <span />
            <span />
          </div>

          <div>
            <strong>DispatchArc</strong>
            <small>Field Service Operations</small>
          </div>
        </div>

        <div className="login-brand-copy">
          <p className="login-eyebrow">
            Operations command center
          </p>

          <h1>
            Every job.
            <br />
            Under control.
          </h1>

          <p>
            Coordinate teams, jobs, customers, invoices and
            field operations from one secure workspace.
          </p>
        </div>

        <div className="login-security">
          <ShieldCheck size={17} />
          <div>
            <strong>Tenant-secured access</strong>
            <span>
              JWT authentication &middot; Role-based permissions
            </span>
          </div>
        </div>
      </section>

      <section className="login-form-panel">
        <div className="login-card">
          <div className="login-icon">
            <LockKeyhole size={20} />
          </div>

          <div className="login-heading">
            <p className="login-eyebrow">Welcome back</p>
            <h2>Sign in to DispatchArc</h2>
            <p>
              Enter your workspace credentials to continue.
            </p>
          </div>

          <form onSubmit={handleSubmit}>
            {workspaceRemembered ? (
              <div className="remembered-workspace">
                <div className="remembered-workspace-icon">
                  <Building2 size={16} />
                </div>

                <div className="remembered-workspace-copy">
                  <span>Workspace remembered</span>
                  <strong>
                    {tenantId.slice(0, 8)}...
                  </strong>
                </div>

                <button
                  onClick={changeWorkspace}
                  type="button"
                >
                  Change
                </button>
              </div>
            ) : (
              <label className="login-field">
                <span>Workspace ID</span>
                <input
                  autoComplete="organization"
                  onChange={(event) =>
                    setTenantId(event.target.value)
                  }
                  placeholder="00000000-0000-0000-0000-000000000000"
                  required
                  type="text"
                  value={tenantId}
                />
              </label>
            )}

            <label className="login-field">
              <span>Email address</span>
              <input
                autoComplete="email"
                onChange={(event) =>
                  setEmail(event.target.value)
                }
                placeholder="you@company.com"
                required
                type="email"
                value={email}
              />
            </label>

            <label className="login-field">
              <span>Password</span>

              <div className="password-field">
                <input
                  autoComplete="current-password"
                  onChange={(event) =>
                    setPassword(event.target.value)
                  }
                  placeholder="Enter your password"
                  required
                  type={showPassword ? 'text' : 'password'}
                  value={password}
                />

                <button
                  aria-label={
                    showPassword
                      ? 'Hide password'
                      : 'Show password'
                  }
                  onClick={() =>
                    setShowPassword((current) => !current)
                  }
                  type="button"
                >
                  {showPassword ? (
                    <EyeOff size={17} />
                  ) : (
                    <Eye size={17} />
                  )}
                </button>
              </div>
            </label>

            {error && (
              <div className="login-error" role="alert">
                {error}
              </div>
            )}

            <button
              className="login-submit"
              disabled={submitting}
              type="submit"
            >
              <span>
                {submitting ? 'Signing in...' : 'Sign in'}
              </span>
              {!submitting && <ArrowRight size={17} />}
            </button>
          </form>

          <p className="login-footnote">
            Access is restricted to authorized DispatchArc
            workspace members.
          </p>
        </div>
      </section>
    </main>
  )
}