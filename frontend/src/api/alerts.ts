import { apiRequest } from './client'

export type OperationalAlertSeverity = 'Info' | 'Warning' | 'Critical'

export type OperationalAlertType =
  | 'AgedQuote'
  | 'OverdueInvoice'
  | 'UnassignedJob'
  | 'SchedulingGap'
  | 'StaleInProgressJob'

export interface OperationalAlertResponse {
  key: string
  type: OperationalAlertType
  audience: string
  severity: OperationalAlertSeverity
  title: string
  message: string
  jobId: string | null
  jobNumber: string | null
  invoiceId: string | null
  invoiceNumber: string | null
  balanceDue: number | null
  relevantAtUtc: string
}

export interface OperationalAlertFeedResponse {
  asOfUtc: string
  totalCount: number
  criticalCount: number
  warningCount: number
  infoCount: number
  alerts: OperationalAlertResponse[]
}

export async function getOperationalAlerts(
  tenantId: string,
): Promise<OperationalAlertFeedResponse> {
  return apiRequest<OperationalAlertFeedResponse>(
    `/api/tenants/${tenantId}/alerts`,
  )
}
