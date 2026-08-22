import { apiRequest } from './client'

export type PaymentMethod =
  | 'Cash'
  | 'Card'
  | 'BankTransfer'
  | 'Online'
  | 'Other'

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
  invoiceId: string
  invoiceNumber: string
  status: 'Issued' | 'PartiallyPaid' | 'Paid' | 'Void'
  invoiceTotal: number
  amountPaid: number
  balanceDue: number
  payments: Payment[]
}

export interface RecordPaymentRequest {
  amount: number
  method: PaymentMethod
  reference?: string
  paidAtUtc?: string
}

export async function getPaymentSummary(
  tenantId: string,
  invoiceId: string,
): Promise<InvoicePaymentSummary> {
  return apiRequest<InvoicePaymentSummary>(
    `/api/tenants/${tenantId}/invoices/${invoiceId}/payments`,
  )
}

export async function recordPayment(
  tenantId: string,
  invoiceId: string,
  request: RecordPaymentRequest,
): Promise<InvoicePaymentSummary> {
  return apiRequest<InvoicePaymentSummary>(
    `/api/tenants/${tenantId}/invoices/${invoiceId}/payments`,
    {
      method: 'POST',
      body: JSON.stringify(request),
    },
  )
}
