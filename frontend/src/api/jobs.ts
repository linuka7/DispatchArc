import { apiRequest } from './client'
import type {
  JobStatus,
  ServiceJob,
} from './types'

export interface GetJobsOptions {
  status?: JobStatus
  search?: string
}

export async function getJobs(
  tenantId: string,
  options: GetJobsOptions = {},
): Promise<ServiceJob[]> {
  const params = new URLSearchParams()

  if (options.status) {
    params.set('status', options.status)
  }

  if (options.search?.trim()) {
    params.set('search', options.search.trim())
  }

  const query = params.toString()

  return apiRequest<ServiceJob[]>(
    `/api/tenants/${tenantId}/jobs${
      query ? `?${query}` : ''
    }`,
  )
}