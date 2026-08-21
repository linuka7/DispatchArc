export type UserRole =
  | 'Owner'
  | 'Dispatcher'
  | 'Technician'
  | 'Finance'

export type JobPriority =
  | 'Low'
  | 'Normal'
  | 'High'
  | 'Urgent'

export type JobStatus =
  | 'New'
  | 'Quoted'
  | 'Approved'
  | 'Scheduled'
  | 'InProgress'
  | 'Completed'
  | 'Invoiced'
  | 'Cancelled'

export type InvoiceStatus =
  | 'Issued'
  | 'PartiallyPaid'
  | 'Paid'
  | 'Void'

export type PaymentMethod =
  | 'Cash'
  | 'Card'
  | 'BankTransfer'
  | 'Cheque'
  | 'Other'

export interface AuthResponse {
  accessToken: string
  expiresAtUtc: string
  userId: string
  tenantId: string
  fullName: string
  email: string
  role: UserRole
}

export interface LoginRequest {
  tenantId: string
  email: string
  password: string
}

export interface ServiceJob {
  id: string
  tenantId: string
  customerId: string
  assignedTechnicianId: string | null
  jobNumber: string
  title: string
  description: string
  priority: JobPriority
  status: JobStatus
  scheduledStartUtc: string | null
  scheduledEndUtc: string | null
  createdAtUtc: string
  updatedAtUtc: string
}

export interface ProblemDetails {
  title?: string
  detail?: string
  status?: number
}

export interface Customer {
  id: string
  tenantId: string
  name: string
  phone: string
  email: string | null
  addressLine: string | null
  city: string | null
  createdAtUtc: string
  updatedAtUtc: string
}

export interface InvoiceLineItem {
  id: string
  tenantId: string
  invoiceId: string
  description: string
  quantity: number
  unitPrice: number
  lineTotal: number
  createdAtUtc: string
}

export interface Invoice {
  id: string
  tenantId: string
  serviceJobId: string
  customerId: string
  invoiceNumber: string
  status: InvoiceStatus
  issuedAtUtc: string
  dueAtUtc: string
  subtotal: number
  total: number
  createdAtUtc: string
  updatedAtUtc: string
  lineItems: InvoiceLineItem[]
}

export interface Payment {
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

export interface InvoicePaymentSummary {
  invoice: Invoice
  payments: Payment[]
  amountPaid: number
  amountDue: number
}