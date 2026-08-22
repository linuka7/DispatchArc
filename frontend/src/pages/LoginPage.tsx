import { useState } from 'react'
import type { FormEvent } from 'react'
import {
  ArrowRight,
  Eye,
  EyeOff,
  LockKeyhole,
  ShieldCheck,
  Waypoints,
} from 'lucide-react'
import { useNavigate } from 'react-router-dom'
import { ApiError } from '../api/client'
import { login, startDemoSession } from '../api/auth'
import './LoginPage.css'

export default function LoginPage() {
  const navigate = useNavigate()

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [error, setError] = useState('')
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    setError('')
    setSubmitting(true)

    try {
      await login({
        email: email.trim(),
        password,
      })

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

  function enterDemo() {
    startDemoSession()
    navigate('/dashboard', { replace: true })
  }

  return (
    <main className="login-page">
      <section className="login-brand-panel">
        <div className="login-brand">
          <div className="login-brand-mark">
            <Waypoints size={20} strokeWidth={2.2} />
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

          <div className="demo-divider"><span>Showcase mode</span></div>
          <button className="demo-button" onClick={enterDemo} type="button">
            Explore live demo
          </button>
        </div>
      </section>
    </main>
  )
}