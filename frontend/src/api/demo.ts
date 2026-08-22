import type { DashboardMetricsResponse } from './dashboard'
import type { OperationalAlertFeedResponse } from './alerts'
import type { Technician, TeamMember } from './technicians'
import type { Invoice, PaymentMethod, ServiceJob, Customer, JobStatus, JobPriority } from './types'

interface DemoPayment {
  id: string
  tenantId: string
  invoiceId: string
  paymentNumber: string
  amount: number
  method: PaymentMethod
  reference: string
  paidAtUtc: string
  createdAtUtc: string
}

interface DemoPaymentSummary {
  invoiceId: string
  invoiceNumber: string
  status: 'Issued' | 'PartiallyPaid' | 'Paid' | 'Void'
  invoiceTotal: number
  amountPaid: number
  balanceDue: number
  payments: DemoPayment[]
}

const demoTenantId = 'demo-tenant'
const demoCustomerId = 'demo-customer-1'
const demoTechnicianId = 'demo-tech-1'
const demoJobId = 'demo-job-1'
const demoInvoiceId = 'demo-invoice-1'

const now = new Date()
const iso = (days = 0) => new Date(now.getTime() + days * 86400000).toISOString()

let customers: Customer[] = [
  { id: demoCustomerId, tenantId: demoTenantId, name: 'Northstar Estates', phone: '+94 77 234 8801', email: 'hello@northstar.example', addressLine: '14 Lake Road', city: 'Colombo', createdAtUtc: iso(-45), updatedAtUtc: iso(-2) },
  { id: 'demo-customer-2', tenantId: demoTenantId, name: 'Cedar & Co.', phone: '+94 71 908 4412', email: 'ops@cedar.example', addressLine: '8 Park Avenue', city: 'Kandy', createdAtUtc: iso(-28), updatedAtUtc: iso(-4) },
  { id: 'demo-customer-3', tenantId: demoTenantId, name: 'Lumen Hospitality', phone: '+94 76 112 9033', email: 'facilities@lumen.example', addressLine: '22 Beach Lane', city: 'Galle', createdAtUtc: iso(-12), updatedAtUtc: iso(-1) },
]

let jobs: ServiceJob[] = [
  { id: demoJobId, tenantId: demoTenantId, customerId: demoCustomerId, assignedTechnicianId: demoTechnicianId, jobNumber: 'JOB-20260823-8F42A1', title: 'HVAC service and inspection', description: 'Quarterly maintenance for the rooftop units.', priority: 'High', status: 'Scheduled', scheduledStartUtc: iso(0), scheduledEndUtc: iso(0), createdAtUtc: iso(-3), updatedAtUtc: iso(-1) },
  { id: 'demo-job-2', tenantId: demoTenantId, customerId: 'demo-customer-2', assignedTechnicianId: null, jobNumber: 'JOB-20260822-4C19D0', title: 'Emergency lighting repair', description: 'Replace failed corridor fixtures on level two.', priority: 'Urgent', status: 'New', scheduledStartUtc: null, scheduledEndUtc: null, createdAtUtc: iso(-1), updatedAtUtc: iso(-1) },
  { id: 'demo-job-3', tenantId: demoTenantId, customerId: 'demo-customer-3', assignedTechnicianId: demoTechnicianId, jobNumber: 'JOB-20260818-76A2BC', title: 'Boiler pressure check', description: 'Pressure reading and safety inspection.', priority: 'Normal', status: 'Completed', scheduledStartUtc: iso(-2), scheduledEndUtc: iso(-2), createdAtUtc: iso(-5), updatedAtUtc: iso(-2) },
  { id: 'demo-job-4', tenantId: demoTenantId, customerId: demoCustomerId, assignedTechnicianId: demoTechnicianId, jobNumber: 'JOB-20260817-19D4EF', title: 'Generator load test', description: 'Annual backup generator inspection and load test.', priority: 'Normal', status: 'Completed', scheduledStartUtc: iso(-4), scheduledEndUtc: iso(-4), createdAtUtc: iso(-7), updatedAtUtc: iso(-4) },
]

const technicians: Technician[] = [{ id: demoTechnicianId, fullName: 'Maya Perera', email: 'maya@dispatcharc.demo' }]
let team: TeamMember[] = [{ id: 'demo-owner', tenantId: demoTenantId, fullName: 'ARK II', email: 'owner@dispatcharc.demo', role: 'Owner', isActive: true, createdAtUtc: iso(-90), updatedAtUtc: iso(-1) }, { id: demoTechnicianId, tenantId: demoTenantId, fullName: 'Maya Perera', email: 'maya@dispatcharc.demo', role: 'Technician', isActive: true, createdAtUtc: iso(-35), updatedAtUtc: iso(-2) }]

let invoices: Invoice[] = [{ id: demoInvoiceId, tenantId: demoTenantId, serviceJobId: 'demo-job-3', customerId: 'demo-customer-3', invoiceNumber: 'INV-20260820-7D91C4A2', status: 'PartiallyPaid', issuedAtUtc: iso(-2), dueAtUtc: iso(28), subtotal: 640, total: 640, createdAtUtc: iso(-2), updatedAtUtc: iso(-1), lineItems: [{ id: 'demo-invoice-line', tenantId: demoTenantId, invoiceId: demoInvoiceId, description: 'Boiler inspection and service', quantity: 1, unitPrice: 640, lineTotal: 640, createdAtUtc: iso(-2) }] }]
let payments: DemoPayment[] = [{ id: 'demo-payment-1', tenantId: demoTenantId, invoiceId: demoInvoiceId, paymentNumber: 'PAY-20260821-001', amount: 320, method: 'Card', reference: 'NSTAR-320', paidAtUtc: iso(-1), createdAtUtc: iso(-1) }]
const quoteItems: Record<string, Array<{ id: string; tenantId: string; serviceJobId: string; description: string; quantity: number; unitPrice: number; lineTotal: number; createdAtUtc: string }>> = { 'demo-job-3': [{ id: 'demo-quote-line', tenantId: demoTenantId, serviceJobId: 'demo-job-3', description: 'Boiler inspection and service', quantity: 1, unitPrice: 640, lineTotal: 640, createdAtUtc: iso(-3) }] }

function bodyOf(init: RequestInit) {
  return init.body ? JSON.parse(String(init.body)) as Record<string, unknown> : {}
}

function metrics(): DashboardMetricsResponse {
  const counts = new Map<JobStatus, number>()
  jobs.forEach((job) => counts.set(job.status, (counts.get(job.status) ?? 0) + 1))
  return { asOfUtc: now.toISOString(), totalCustomers: customers.length, activeTechnicians: technicians.length, totalJobs: jobs.length, openJobs: jobs.filter((job) => !['Completed', 'Invoiced', 'Cancelled'].includes(job.status)).length, scheduledToday: jobs.filter((job) => job.status === 'Scheduled').length, jobsByStatus: [...counts.entries()].map(([status, count]) => ({ status, count })), totalInvoiced: invoices.reduce((sum, invoice) => sum + invoice.total, 0), totalCollected: payments.reduce((sum, payment) => sum + payment.amount, 0), collectedThisMonth: payments.reduce((sum, payment) => sum + payment.amount, 0), outstandingInvoiceCount: invoices.filter((invoice) => invoice.status === 'PartiallyPaid' || invoice.status === 'Issued').length, outstandingBalance: invoices.reduce((sum, invoice) => sum + invoice.total, 0) - payments.reduce((sum, payment) => sum + payment.amount, 0), overdueInvoiceCount: 0, overdueBalance: 0 }
}

function paymentSummary(invoice: Invoice): DemoPaymentSummary {
  const invoicePayments = payments.filter((payment) => payment.invoiceId === invoice.id)
  const amountPaid = invoicePayments.reduce((sum, payment) => sum + payment.amount, 0)
  return { invoiceId: invoice.id, invoiceNumber: invoice.invoiceNumber, status: amountPaid >= invoice.total ? 'Paid' : amountPaid > 0 ? 'PartiallyPaid' : 'Issued', invoiceTotal: invoice.total, amountPaid, balanceDue: Math.max(0, invoice.total - amountPaid), payments: invoicePayments }
}

export async function demoRequest<T>(path: string, init: RequestInit = {}): Promise<T> {
  const [rawPath, rawQuery] = path.split('?')
  const parts = rawPath.split('/').filter(Boolean)
  const query = new URLSearchParams(rawQuery)
  const method = init.method ?? 'GET'
  const body = bodyOf(init)

  if (parts.at(-1) === 'dashboard') return metrics() as T
  if (parts.at(-1) === 'alerts') return { asOfUtc: now.toISOString(), totalCount: 1, criticalCount: 0, warningCount: 1, infoCount: 0, alerts: [{ key: 'demo-alert', type: 'UnassignedJob', audience: 'Operations', severity: 'Warning', title: 'New job needs assignment', message: 'Emergency lighting repair is waiting for a technician.', jobId: 'demo-job-2', jobNumber: 'JOB-20260822-4C19D0', invoiceId: null, invoiceNumber: null, balanceDue: null, relevantAtUtc: iso(-1) }] } satisfies OperationalAlertFeedResponse as T
  if (parts.at(-1) === 'technicians') return technicians as T
  if (parts.at(-1) === 'team-members') {
    if (method === 'POST') { const member = { id: `demo-member-${Date.now()}`, tenantId: demoTenantId, fullName: String(body.fullName), email: String(body.email), role: 'Technician' as const, isActive: true, createdAtUtc: now.toISOString(), updatedAtUtc: now.toISOString() }; team = [...team, member]; return member as T }
    return team as T
  }
  if (parts.at(-1) === 'customers') {
    if (method === 'POST') { const customer = { id: `demo-customer-${Date.now()}`, tenantId: demoTenantId, name: String(body.name), phone: String(body.phone), email: body.email ? String(body.email) : null, addressLine: body.addressLine ? String(body.addressLine) : null, city: body.city ? String(body.city) : null, createdAtUtc: now.toISOString(), updatedAtUtc: now.toISOString() }; customers = [customer, ...customers]; return customer as T }
    const search = query.get('search')?.toLowerCase() ?? ''
    return customers.filter((customer) => !search || `${customer.name} ${customer.email ?? ''} ${customer.phone}`.toLowerCase().includes(search)) as T
  }
  if (parts.includes('jobs')) {
    const jobId = parts.find((part) => part.startsWith('demo-job'))
    if (parts.at(-1) === 'jobs' || (parts.at(-2) === 'jobs')) {
      if (method === 'POST' && !jobId) { const job = { id: `demo-job-${Date.now()}`, tenantId: demoTenantId, customerId: String(body.customerId), assignedTechnicianId: null, jobNumber: `JOB-20260823-${Math.random().toString(16).slice(2, 8).toUpperCase()}`, title: String(body.title), description: String(body.description ?? ''), priority: String(body.priority) as JobPriority, status: 'New' as const, scheduledStartUtc: null, scheduledEndUtc: null, createdAtUtc: now.toISOString(), updatedAtUtc: now.toISOString() }; jobs = [job, ...jobs]; return job as T }
      const status = query.get('status')
      const search = query.get('search')?.toLowerCase() ?? ''
      return jobs.filter((job) => (!status || job.status === status) && (!search || `${job.jobNumber} ${job.title} ${job.description}`.toLowerCase().includes(search))) as T
    }
    if (jobId && parts.at(-1) === jobId) return jobs.find((job) => job.id === jobId) as T
    if (jobId && parts.at(-1) === 'line-items' && method === 'POST') {
      const item = { id: `demo-quote-line-${Date.now()}`, tenantId: demoTenantId, serviceJobId: jobId, description: String(body.description), quantity: Number(body.quantity), unitPrice: Number(body.unitPrice), lineTotal: Number(body.quantity) * Number(body.unitPrice), createdAtUtc: now.toISOString() }
      quoteItems[jobId] = [...(quoteItems[jobId] ?? []), item]
      return item as T
    }
    if (jobId && ['quote', 'approve', 'start', 'complete', 'cancel'].includes(parts.at(-1) ?? '') && method === 'POST') {
      const action = parts.at(-1)
      const nextStatus: Record<string, JobStatus> = { quote: 'Quoted', approve: 'Approved', start: 'InProgress', complete: 'Completed', cancel: 'Cancelled' }
      const job = jobs.find((item) => item.id === jobId)!
      const updated = { ...job, status: nextStatus[action!] ?? job.status, updatedAtUtc: now.toISOString() }
      jobs = jobs.map((item) => item.id === jobId ? updated : item)
      return updated as T
    }
    if (jobId && parts.at(-1) === 'assign-technician' && method === 'POST') {
      const job = jobs.find((item) => item.id === jobId)!
      const updated = { ...job, assignedTechnicianId: String(body.technicianId), updatedAtUtc: now.toISOString() }
      jobs = jobs.map((item) => item.id === jobId ? updated : item)
      return updated as T
    }
    if (jobId && parts.at(-1) === 'schedule' && method === 'POST') {
      const job = jobs.find((item) => item.id === jobId)!
      const updated = { ...job, status: 'Scheduled' as const, scheduledStartUtc: String(body.startUtc), scheduledEndUtc: String(body.endUtc), updatedAtUtc: now.toISOString() }
      jobs = jobs.map((item) => item.id === jobId ? updated : item)
      return updated as T
    }
    if (jobId && parts.includes('quote') && parts.at(-1) === 'line-items') {
      const lineItems = quoteItems[jobId] ?? []
      return { tenantId: demoTenantId, serviceJobId: jobId, lineItems, subtotal: lineItems.reduce((sum, item) => sum + item.lineTotal, 0) } as T
    }
    if (jobId && parts.includes('quote')) return { tenantId: demoTenantId, serviceJobId: jobId, lineItems: [], subtotal: 0 } as T
  }
  if (method === 'POST' && parts.at(-1) === 'invoice') {
    const jobId = parts.find((part) => part.startsWith('demo-job'))!
    const job = jobs.find((item) => item.id === jobId)!
    const invoiceId = `demo-invoice-${Date.now()}`
    const lineItem = { id: `demo-invoice-line-${Date.now()}`, tenantId: demoTenantId, invoiceId, description: job.title, quantity: 1, unitPrice: 480, lineTotal: 480, createdAtUtc: now.toISOString() }
    const invoice: Invoice = { id: invoiceId, tenantId: demoTenantId, serviceJobId: jobId, customerId: job.customerId, invoiceNumber: `INV-20260823-${Math.random().toString(16).slice(2, 10).toUpperCase()}`, status: 'Issued', issuedAtUtc: now.toISOString(), dueAtUtc: iso(30), subtotal: 480, total: 480, createdAtUtc: now.toISOString(), updatedAtUtc: now.toISOString(), lineItems: [lineItem] }
    invoices = [...invoices, invoice]
    return invoice as T
  }
  if (parts.includes('invoices')) {
    const invoiceId = parts.find((part) => part.startsWith('demo-invoice'))
    if (invoiceId && parts.at(-1) === 'payments') {
      const invoice = invoices.find((item) => item.id === invoiceId)!
      if (method === 'POST') {
        const payment = { id: `demo-payment-${Date.now()}`, tenantId: demoTenantId, invoiceId, paymentNumber: `PAY-${Date.now()}`, amount: Number(body.amount), method: String(body.method) as PaymentMethod, reference: String(body.reference ?? ''), paidAtUtc: now.toISOString(), createdAtUtc: now.toISOString() }
        payments = [...payments, payment]
      }
      return paymentSummary(invoice) as T
    }
    if (invoiceId) return invoices.find((invoice) => invoice.id === invoiceId) as T
  }
  if (parts.includes('invoice') && parts.at(-1) === 'invoice') return (invoices.find((invoice) => invoice.serviceJobId === parts.find((part) => part.startsWith('demo-job'))) ?? null) as T
  return [] as T
}

export { demoTenantId }
