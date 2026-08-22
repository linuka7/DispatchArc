import { useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import {
  BriefcaseBusiness,
  CalendarDays,
  ChevronRight,
  CircleDollarSign,
  Clock3,
  RefreshCw,
  ShieldAlert,
  Wrench,
} from 'lucide-react'
import { getJobs } from '../api/jobs'
import { getCustomers } from '../api/customers'
import { getTechnicians } from '../api/technicians'
import { getDashboardMetrics } from '../api/dashboard'
import { getOperationalAlerts } from '../api/alerts'
import type { AuthResponse, ServiceJob, Customer } from '../api/types'
import './DashboardPage.css'

interface DashboardPageProps {
  tenantId: string
  session: AuthResponse
}

function formatMoney(value: number) {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
    maximumFractionDigits: 0,
  }).format(value)
}

function formatJobTime(job: ServiceJob): string {
  if (job.scheduledStartUtc) {
    try {
      return new Date(job.scheduledStartUtc).toLocaleTimeString([], {
        hour: '2-digit',
        minute: '2-digit',
      })
    } catch {
      // Fallback
    }
  }
  return 'Flexible'
}

function DashboardPage({ tenantId, session }: DashboardPageProps) {
  const isOwner = session.role === 'Owner'
  const canViewAlerts = session.role === 'Owner' || session.role === 'Dispatcher' || session.role === 'Finance'

  // 1. Dashboard Metrics Query (Owner endpoint)
  const dashboardQuery = useQuery({
    queryKey: ['dashboard-metrics', tenantId],
    queryFn: () => getDashboardMetrics(tenantId),
    enabled: isOwner,
  })

  // 2. Jobs Query
  const jobsQuery = useQuery({
    queryKey: ['jobs', tenantId],
    queryFn: () => getJobs(tenantId),
  })

  // 3. Customers Query (for mapping customer names to jobs)
  const customersQuery = useQuery({
    queryKey: ['customers', tenantId],
    queryFn: () => getCustomers(tenantId),
  })

  // 4. Technicians Query
  const techniciansQuery = useQuery({
    queryKey: ['technicians', tenantId],
    queryFn: () => getTechnicians(tenantId),
  })

  // 5. Operational Alerts Query
  const alertsQuery = useQuery({
    queryKey: ['operational-alerts', tenantId],
    queryFn: () => getOperationalAlerts(tenantId),
    enabled: canViewAlerts,
  })

  const jobs = useMemo(() => jobsQuery.data ?? [], [jobsQuery.data])
  const customers = useMemo(() => customersQuery.data ?? [], [customersQuery.data])
  const technicians = useMemo(() => techniciansQuery.data ?? [], [techniciansQuery.data])
  const alerts = useMemo(() => alertsQuery.data?.alerts ?? [], [alertsQuery.data])

  const customerMap = useMemo(() => {
    const map = new Map<string, Customer>()
    for (const customer of customers) {
      map.set(customer.id, customer)
    }
    return map
  }, [customers])

  // Computed metrics (fallback or for non-owner roles)
  const activeJobsCount = useMemo(() => {
    if (dashboardQuery.data) return dashboardQuery.data.openJobs
    return jobs.filter(
      (j) => j.status !== 'Completed' && j.status !== 'Invoiced' && j.status !== 'Cancelled',
    ).length
  }, [dashboardQuery.data, jobs])

  const awaitingApprovalCount = useMemo(() => {
    return jobs.filter((j) => j.status === 'Quoted' || j.status === 'New').length
  }, [jobs])

  const openInvoicesCount = useMemo(() => {
    if (dashboardQuery.data) return dashboardQuery.data.outstandingInvoiceCount
    return jobs.filter((j) => j.status === 'Invoiced').length
  }, [dashboardQuery.data, jobs])

  const outstandingBalanceAmount = useMemo(() => {
    if (dashboardQuery.data) return dashboardQuery.data.outstandingBalance
    return 0
  }, [dashboardQuery.data])

  const totalTechniciansCount = useMemo(() => {
    if (dashboardQuery.data) return dashboardQuery.data.activeTechnicians
    return technicians.length
  }, [dashboardQuery.data, technicians])

  const scheduledTodayCount = useMemo(() => {
    if (dashboardQuery.data) return dashboardQuery.data.scheduledToday
    return jobs.filter((j) => j.status === 'Scheduled').length
  }, [dashboardQuery.data, jobs])

  // Top 5 live queue jobs
  const liveQueueJobs = useMemo(() => {
    return jobs.slice(0, 6)
  }, [jobs])

  const todayGreeting = useMemo(() => {
    const now = new Date()
    const hour = now.getHours()
    if (hour < 12) return 'Good morning'
    if (hour < 17) return 'Good afternoon'
    return 'Good evening'
  }, [])

  const currentDayName = useMemo(() => {
    return new Date().toLocaleDateString('en-US', { weekday: 'long' })
  }, [])

  function refetchAll() {
    if (isOwner) dashboardQuery.refetch()
    jobsQuery.refetch()
    customersQuery.refetch()
    techniciansQuery.refetch()
    if (canViewAlerts) alertsQuery.refetch()
  }

  return (
    <main className="dashboard dashboard-workspace">
      {/* Hero Header */}
      <section className="dashboard-hero-row">
        <div>
          <p className="eyebrow">{currentDayName} &middot; Operations</p>
          <h1>{todayGreeting}, {session.fullName.split(' ')[0]}.</h1>
          <p className="hero-copy">Here&rsquo;s what needs your attention across today&rsquo;s dispatch.</p>
        </div>

        <div className="dashboard-hero-actions">
          <button
            className="secondary-button"
            onClick={refetchAll}
            title="Refresh dashboard"
            type="button"
          >
            <RefreshCw size={16} />
            Refresh
          </button>
        </div>
      </section>

      {/* KPI Metrics Grid */}
      <section className="dashboard-metric-grid">
        <article className="dashboard-metric-card dashboard-metric-card-featured">
          <div className="dashboard-metric-icon">
            <BriefcaseBusiness size={20} />
          </div>
          <div>
            <span className="dashboard-metric-label">Active jobs</span>
            <strong>{activeJobsCount}</strong>
            <small>{scheduledTodayCount} scheduled today</small>
          </div>
        </article>

        <article className="dashboard-metric-card">
          <div className="dashboard-metric-icon">
            <Clock3 size={20} />
          </div>
          <div>
            <span className="dashboard-metric-label">Awaiting approval</span>
            <strong>{awaitingApprovalCount}</strong>
            <small>{jobs.filter(j => j.status === 'Quoted').length} quoted</small>
          </div>
        </article>

        <article className="dashboard-metric-card">
          <div className="dashboard-metric-icon">
            <CircleDollarSign size={20} />
          </div>
          <div>
            <span className="dashboard-metric-label">Open invoices</span>
            <strong>{openInvoicesCount}</strong>
            <small>
              {outstandingBalanceAmount > 0
                ? `${formatMoney(outstandingBalanceAmount)} outstanding`
                : `${jobs.filter(j => j.status === 'Invoiced').length} in billing`}
            </small>
          </div>
        </article>

        <article className="dashboard-metric-card">
          <div className="dashboard-metric-icon">
            <Wrench size={20} />
          </div>
          <div>
            <span className="dashboard-metric-label">Technicians</span>
            <strong>{totalTechniciansCount}</strong>
            <small>{technicians.length} active in team</small>
          </div>
        </article>
      </section>

      {/* Live Queue & Priority Alert Feed */}
      <section className="dashboard-grid-layout">
        {/* Live Queue */}
        <article className="dashboard-panel">
          <header className="dashboard-panel-header">
            <div>
              <p className="eyebrow">Live queue</p>
              <h2>Current jobs</h2>
            </div>

            <Link className="text-button" to="/jobs">
              View all
              <ChevronRight size={15} />
            </Link>
          </header>

          {jobsQuery.isLoading ? (
            <div className="dashboard-empty-panel">
              <p>Loading live queue...</p>
            </div>
          ) : liveQueueJobs.length === 0 ? (
            <div className="dashboard-empty-panel">
              <BriefcaseBusiness size={28} />
              <strong>No jobs in queue</strong>
              <p>Create a new service job to get started.</p>
            </div>
          ) : (
            <div className="job-list">
              {liveQueueJobs.map((job) => {
                const customer = customerMap.get(job.customerId)
                return (
                  <Link
                    className="dashboard-job-row"
                    key={job.id}
                    to="/jobs"
                  >
                    <div className="dashboard-job-time">
                      <CalendarDays size={15} />
                      <span>{formatJobTime(job)}</span>
                    </div>

                    <div className="dashboard-job-main">
                      <span className="dashboard-job-number">{job.jobNumber}</span>
                      <strong>{job.title}</strong>
                      <small>{customer?.name ?? 'Customer'}</small>
                    </div>

                    <span
                      className={`status-pill status-${job.status
                        .toLowerCase()
                        .replace(' ', '-')}`}
                    >
                      {job.status}
                    </span>
                  </Link>
                )
              })}
            </div>
          )}
        </article>

        {/* Priority & Operational Alerts */}
        <aside className="dashboard-panel">
          <header className="dashboard-panel-header">
            <div>
              <p className="eyebrow">Priority</p>
              <h2>Needs attention</h2>
            </div>

            {alerts.length > 0 && (
              <span style={{ fontSize: '12px', color: 'var(--muted)' }}>
                {alerts.length} alert{alerts.length === 1 ? '' : 's'}
              </span>
            )}
          </header>

          {alertsQuery.isLoading ? (
            <div className="dashboard-empty-panel">
              <p>Checking alerts...</p>
            </div>
          ) : alerts.length === 0 ? (
            <div className="dashboard-empty-panel">
              <ShieldAlert size={28} />
              <strong>All clear</strong>
              <p>No operational issues currently requiring attention.</p>
            </div>
          ) : (
            <div>
              {alerts.slice(0, 5).map((alert) => (
                <div className="dashboard-alert-item" key={alert.key}>
                  <span
                    className={`dashboard-alert-dot ${alert.severity.toLowerCase()}`}
                  />
                  <div className="dashboard-alert-content">
                    <strong>{alert.title}</strong>
                    <p>{alert.message}</p>
                  </div>
                </div>
              ))}
            </div>
          )}
        </aside>
      </section>
    </main>
  )
}

export default DashboardPage
