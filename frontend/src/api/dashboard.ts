import { apiRequest } from './client'
import type { JobStatus } from './types'

export interface DashboardJobStatusResponse {
  status: JobStatus
  count: number
}

export interface DashboardMetricsResponse {
  asOfUtc: string
  totalCustomers: number
  activeTechnicians: number
  totalJobs: number
  openJobs: number
  scheduledToday: number
  jobsByStatus: DashboardJobStatusResponse[]
  totalInvoiced: number
  totalCollected: number
  collectedThisMonth: number
  outstandingInvoiceCount: number
  outstandingBalance: number
  overdueInvoiceCount: number
  overdueBalance: number
}

export async function getDashboardMetrics(
  tenantId: string,
): Promise<DashboardMetricsResponse> {
  return apiRequest<DashboardMetricsResponse>(
    `/api/tenants/${tenantId}/dashboard`,
  )
}
