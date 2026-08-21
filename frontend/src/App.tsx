import { useState } from 'react'
import {
  Bell,
  BriefcaseBusiness,
  CalendarDays,
  ChevronRight,
  CircleDollarSign,
  Clock3,
  LayoutDashboard,
  Menu,
  Plus,
  ReceiptText,
  Search,
  UserRoundCog,
  Users,
  WalletCards,
  Wrench,
  LogOut,
  X,
} from 'lucide-react'
import { NavLink, Navigate, Route, Routes, useLocation } from 'react-router-dom'
import './App.css'
import LoginPage from './pages/LoginPage'
import JobsPage from './pages/JobsPage'
import { getCurrentSession, logout } from './api/auth'

const navigation = [
  { label: 'Overview', path: '/dashboard', icon: LayoutDashboard },
  { label: 'Jobs', path: '/jobs', icon: BriefcaseBusiness },
  { label: 'Customers', path: '/customers', icon: Users },
  { label: 'Team', path: '/team', icon: UserRoundCog },
  { label: 'Invoices', path: '/invoices', icon: ReceiptText },
  { label: 'Payments', path: '/payments', icon: WalletCards },
]

const jobs = [
  {
    number: 'JOB-24018',
    title: 'HVAC inspection & repair',
    customer: 'Northstar Properties',
    status: 'Scheduled',
    time: '09:30 AM',
  },
  {
    number: 'JOB-24019',
    title: 'Electrical panel diagnosis',
    customer: 'Atlas Retail Group',
    status: 'In Progress',
    time: '11:00 AM',
  },
  {
    number: 'JOB-24020',
    title: 'Emergency plumbing callout',
    customer: 'Harbour Suites',
    status: 'Approved',
    time: '01:45 PM',
  },
]

function DashboardPage() {
  return (
    <main className="dashboard">
      <section className="hero-row">
        <div>
          <p className="eyebrow">Wednesday &middot; Operations</p>
          <h1>Good evening.</h1>
          <p className="hero-copy">Here&rsquo;s what needs your attention across today&rsquo;s dispatch.</p>
        </div>

        <button className="primary-button" type="button">
          <Plus size={17} />
          New job
        </button>
      </section>

      <section className="metric-grid">
        <article className="metric-card metric-card-featured">
          <div className="metric-icon">
            <BriefcaseBusiness size={19} />
          </div>
          <div>
            <span className="metric-label">Active jobs</span>
            <strong>18</strong>
            <small>6 scheduled today</small>
          </div>
        </article>

        <article className="metric-card">
          <div className="metric-icon">
            <Clock3 size={19} />
          </div>
          <div>
            <span className="metric-label">Awaiting approval</span>
            <strong>05</strong>
            <small>2 need follow-up</small>
          </div>
        </article>

        <article className="metric-card">
          <div className="metric-icon">
            <CircleDollarSign size={19} />
          </div>
          <div>
            <span className="metric-label">Open invoices</span>
            <strong>12</strong>
            <small>$8,420 outstanding</small>
          </div>
        </article>

        <article className="metric-card">
          <div className="metric-icon">
            <Wrench size={19} />
          </div>
          <div>
            <span className="metric-label">Technicians</span>
            <strong>08</strong>
            <small>6 currently available</small>
          </div>
        </article>
      </section>

      <section className="dashboard-grid">
        <article className="panel jobs-panel">
          <header className="panel-header">
            <div>
              <p className="eyebrow">Live queue</p>
              <h2>Today&rsquo;s jobs</h2>
            </div>

            <button className="text-button" type="button">
              View all
              <ChevronRight size={15} />
            </button>
          </header>

          <div className="job-list">
            {jobs.map((job) => (
              <div className="job-row" key={job.number}>
                <div className="job-time">
                  <CalendarDays size={16} />
                  <span>{job.time}</span>
                </div>

                <div className="job-main">
                  <span className="job-number">{job.number}</span>
                  <strong>{job.title}</strong>
                  <small>{job.customer}</small>
                </div>

                <span
                  className={`status-pill status-${job.status
                    .toLowerCase()
                    .replace(' ', '-')}`}
                >
                  {job.status}
                </span>
              </div>
            ))}
          </div>
        </article>

        <aside className="panel attention-panel">
          <header className="panel-header">
            <div>
              <p className="eyebrow">Priority</p>
              <h2>Needs attention</h2>
            </div>
          </header>

          <div className="attention-item">
            <span className="attention-dot" />
            <div>
              <strong>2 quotes are ageing</strong>
              <p>Waiting more than 48 hours for customer approval.</p>
            </div>
          </div>

          <div className="attention-item">
            <span className="attention-dot warning" />
            <div>
              <strong>Invoice #INV-1042</strong>
              <p>Payment is now 6 days overdue.</p>
            </div>
          </div>

          <div className="attention-item">
            <span className="attention-dot calm" />
            <div>
              <strong>Schedule opening</strong>
              <p>Two technicians are free after 3:00 PM.</p>
            </div>
          </div>
        </aside>
      </section>
    </main>
  )
}

function PlaceholderPage({ title }: { title: string }) {
  return (
    <main className="dashboard">
      <section className="placeholder-page">
        <p className="eyebrow">DispatchArc</p>
        <h1>{title}</h1>
        <p>This workspace is ready for the next M20 build step.</p>
      </section>
    </main>
  )
}

function App() {
  const [mobileNavOpen, setMobileNavOpen] = useState(false)
  const location = useLocation()
  const session = getCurrentSession()

  const userInitials = session
    ? session.fullName
        .split(/\s+/)
        .filter(Boolean)
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

  return (
    <div className="app-shell">
      <aside className={`sidebar ${mobileNavOpen ? 'sidebar-open' : ''}`}>
        <div className="brand-row">
          <div className="brand-mark">
            <span />
            <span />
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
              onClick={() => setMobileNavOpen(false)}
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

            <div className="search-box">
              <Search size={17} />
              <input
                aria-label="Search DispatchArc"
                placeholder="Search jobs, customers, invoices..."
                type="search"
              />
              <kbd>? K</kbd>
            </div>
          </div>

          <div className="topbar-actions">
            <button aria-label="Notifications" className="icon-button" type="button">
              <Bell size={18} />
              <span className="notification-dot" />
            </button>

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
          <Route path="/dashboard" element={<DashboardPage />} />
          <Route path="/jobs" element={<JobsPage tenantId={session.tenantId} />} />
          <Route
            path="/customers"
            element={<PlaceholderPage title="Customers" />}
          />
          <Route path="/team" element={<PlaceholderPage title="Team" />} />
          <Route
            path="/invoices"
            element={<PlaceholderPage title="Invoices" />}
          />
          <Route
            path="/payments"
            element={<PlaceholderPage title="Payments" />}
          />
          <Route path="*" element={<Navigate replace to="/dashboard" />} />
        </Routes>
      </div>
    </div>
  )
}

export default App
