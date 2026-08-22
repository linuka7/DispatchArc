import { useEffect, useState } from 'react'
import {
  Bell,
  BriefcaseBusiness,
  LayoutDashboard,
  Menu,
  ReceiptText,
  Search,
  UserRoundCog,
  Users,
  WalletCards,
  LogOut,
  X,
  Waypoints,
} from 'lucide-react'
import { NavLink, Navigate, Route, Routes, useLocation, useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import './App.css'
import LoginPage from './pages/LoginPage'
import DashboardPage from './pages/DashboardPage'
import JobsPage from './pages/JobsPage'
import TeamMembersPage from './pages/TeamMembersPage'
import { getCurrentSession, logout } from './api/auth'
import { getOperationalAlerts } from './api/alerts'
import InvoicesPage from './pages/InvoicesPage'
import PaymentsPage from './pages/PaymentsPage'
import CustomersPage from './pages/CustomersPage'

const navigation = [
  { label: 'Overview', path: '/dashboard', icon: LayoutDashboard },
  { label: 'Jobs', path: '/jobs', icon: BriefcaseBusiness },
  { label: 'Customers', path: '/customers', icon: Users },
  { label: 'Team', path: '/team', icon: UserRoundCog },
  { label: 'Invoices', path: '/invoices', icon: ReceiptText },
  { label: 'Payments', path: '/payments', icon: WalletCards },
]





function App() {
  const [mobileNavOpen, setMobileNavOpen] = useState(false)
  const [notificationsOpen, setNotificationsOpen] = useState(false)
  const [globalSearch, setGlobalSearch] = useState('')
  const location = useLocation()
  const navigate = useNavigate()
  const session = getCurrentSession()

  const canViewAlerts = Boolean(
    session &&
      (session.role === 'Owner' ||
        session.role === 'Dispatcher' ||
        session.role === 'Finance'),
  )

  const alertsQuery = useQuery({
    queryKey: ['topbar-alerts', session?.tenantId],
    queryFn: () => getOperationalAlerts(session!.tenantId),
    enabled: canViewAlerts,
  })

  useEffect(() => {
    if (!notificationsOpen) {
      return
    }

    function closeNotifications() {
      setNotificationsOpen(false)
    }

    document.addEventListener('click', closeNotifications)
    return () => document.removeEventListener('click', closeNotifications)
  }, [notificationsOpen])

  const userInitials = session
    ? session.fullName
        .split(/\s+/)
        .slice(0, 2)
        .map((part) => part[0]?.toUpperCase())
        .join('')
    : 'DA'

  if (location.pathname === '/login') {
    return session ? (
      <Navigate replace to="/dashboard" />
    ) : (
      <LoginPage />
    )
  }

  if (!session) {
    return <Navigate replace to="/login" />
  }

  const alertCount = alertsQuery.data?.totalCount ?? 0

  function handleGlobalSearchSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const search = globalSearch.trim()

    if (search) {
      navigate(`/jobs?search=${encodeURIComponent(search)}`)
    }
  }

  return (
    <div className="app-shell">
      <aside className={`sidebar ${mobileNavOpen ? 'sidebar-open' : ''}`}>
        <div className="brand-row">
          <div className="brand-mark">
              <Waypoints size={19} strokeWidth={2.2} />
          </div>
          <div className="brand-copy">
            <strong>DispatchArc</strong>
            <small>Operations</small>
          </div>

          <button
            aria-label="Close navigation"
            className="mobile-close"
            onClick={() => setMobileNavOpen(false)}
            type="button"
          >
            <X size={20} />
          </button>
        </div>

        <nav className="side-nav">
          <span className="nav-heading">Workspace</span>

          {navigation.map(({ label, path, icon: Icon }) => (
            <NavLink
              className={({ isActive }) =>
                `nav-item ${isActive ? 'nav-item-active' : ''}`
              }
              key={path}
              onClick={() => {
                setMobileNavOpen(false)
                setGlobalSearch('')
                setNotificationsOpen(false)
              }}
              to={path}
            >
              <Icon size={18} />
              <span>{label}</span>
            </NavLink>
          ))}
        </nav>

        <div className="sidebar-footer">
          <div className="workspace-card">
            <div className="workspace-avatar">DA</div>
            <div>
              <strong>DispatchArc HQ</strong>
              <small>{session.role} workspace</small>
            </div>
          </div>
        </div>
      </aside>

      {mobileNavOpen && (
        <button
          aria-label="Close navigation overlay"
          className="sidebar-overlay"
          onClick={() => setMobileNavOpen(false)}
          type="button"
        />
      )}

      <div className="app-content">
        <header className="topbar">
          <div className="topbar-left">
            <button
              aria-label="Open navigation"
              className="mobile-menu"
              onClick={() => setMobileNavOpen(true)}
              type="button"
            >
              <Menu size={20} />
            </button>

            <form className="search-box" onSubmit={handleGlobalSearchSubmit}>
              <Search size={17} />
              <input
                aria-label="Search DispatchArc"
                onChange={(event) => setGlobalSearch(event.target.value)}
                placeholder="Search jobs, customers, invoices..."
                type="search"
                value={globalSearch}
              />
            </form>
          </div>
          <div className="topbar-actions">
            <div className="notification-wrap">
              <button
                aria-expanded={notificationsOpen}
                aria-label="Notifications"
                className="icon-button"
                onClick={(event) => {
                  event.stopPropagation()
                  setNotificationsOpen((current) => !current)
                }}
                title={alertCount > 0 ? `${alertCount} operational alerts` : 'Notifications'}
                type="button"
              >
                <Bell size={18} />
                {alertCount > 0 && <span className="notification-dot" />}
              </button>

              {notificationsOpen && (
                <div className="notification-panel" onClick={(event) => event.stopPropagation()}>
                  <div className="notification-panel-header">
                    <div>
                      <span className="eyebrow">Operations</span>
                      <strong>Notifications</strong>
                    </div>
                    <span>{alertCount} active</span>
                  </div>

                  {alertsQuery.isLoading ? (
                    <p className="notification-empty">Checking for alerts...</p>
                  ) : alertsQuery.isError ? (
                    <p className="notification-empty">Alerts are unavailable right now.</p>
                  ) : alertCount === 0 ? (
                    <p className="notification-empty">Everything is clear.</p>
                  ) : (
                    <div className="notification-list">
                      {alertsQuery.data?.alerts.slice(0, 5).map((alert) => (
                        <NavLink
                          className="notification-item"
                          key={alert.key}
                          onClick={() => setNotificationsOpen(false)}
                          to="/dashboard"
                        >
                          <span className={`notification-severity ${alert.severity.toLowerCase()}`} />
                          <span>
                            <strong>{alert.title}</strong>
                            <small>{alert.message}</small>
                          </span>
                        </NavLink>
                      ))}
                    </div>
                  )}
                </div>
              )}
            </div>

            <div className="user-chip">
              <div className="user-avatar">{userInitials}</div>
              <div className="user-copy">
                <strong>{session.fullName}</strong>
                <small>{session.role}</small>
              </div>
            </div>

            <button
              aria-label="Sign out"
              className="icon-button"
              onClick={() => {
                logout()
                window.location.replace('/login')
              }}
              title="Sign out"
              type="button"
            >
              <LogOut size={18} />
            </button>
          </div>
        </header>

        <Routes>
          <Route path="/" element={<Navigate replace to="/dashboard" />} />
          <Route
            path="/dashboard"
            element={
              <DashboardPage
                session={session}
                tenantId={session.tenantId}
              />
            }
          />
          <Route
            path="/jobs"
            element={<JobsPage key={location.search} tenantId={session.tenantId} />}
          />
          <Route
            path="/customers"
            element={<CustomersPage tenantId={session.tenantId} />}
          />
          <Route
            path="/team"
            element={<TeamMembersPage tenantId={session.tenantId} />}
          />
          <Route
            path="/invoices"
            element={<InvoicesPage tenantId={session.tenantId} />}
          />
          <Route
            path="/payments"
            element={<PaymentsPage tenantId={session.tenantId} />}
          />
          <Route path="*" element={<Navigate replace to="/dashboard" />} />
        </Routes>
      </div>
    </div>
  )
}

export default App
