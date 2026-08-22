import { apiRequest } from './client'


export type InvoiceStatus =
  | 'Issued'
  | 'PartiallyPaid'
  | 'Paid'
  | 'Void'

export interface InvoiceLineItem {
  id: string
  description: string
  quantity: number
  unitPrice: number
  lineTotal: number
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
  lineItems: InvoiceLineItem[]
  createdAtUtc: string
  updatedAtUtc: string
}

export interface CreateInvoiceRequest {
  dueAtUtc?: string
}

export async function getInvoiceByJob(
  tenantId: string,
  jobId: string,
): Promise<Invoice> {
  return apiRequest<Invoice>(
    `/api/tenants/${tenantId}/jobs/${jobId}/invoice`,
  )
}

export async function getInvoiceById(
  tenantId: string,
  invoiceId: string,
): Promise<Invoice> {
  return apiRequest<Invoice>(
    `/api/tenants/${tenantId}/invoices/${invoiceId}`,
  )
}

export async function createInvoice(
  tenantId: string,
  jobId: string,
  request: CreateInvoiceRequest = {},
): Promise<Invoice> {
  return apiRequest<Invoice>(
    `/api/tenants/${tenantId}/jobs/${jobId}/invoice`,
    {
      method: 'POST',
      body: JSON.stringify(request),
    },
  )
}