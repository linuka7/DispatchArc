import { apiRequest } from './client'
import type {
  JobPriority,
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

export async function getJobById(
  tenantId: string,
  jobId: string,
): Promise<ServiceJob> {
  return apiRequest<ServiceJob>(
    `/api/tenants/${tenantId}/jobs/${jobId}`,
  )
}

export interface CreateJobRequest {
  customerId: string
  title: string
  description: string
  priority: JobPriority
}

export async function createJob(
  tenantId: string,
  request: CreateJobRequest,
): Promise<ServiceJob> {
  return apiRequest<ServiceJob>(
    `/api/tenants/${tenantId}/jobs`,
    {
      method: 'POST',
      body: JSON.stringify(request),
    },
  )
}

export interface AssignTechnicianRequest {
  technicianId: string
}

export async function assignTechnician(
  tenantId: string,
  jobId: string,
  technicianId: string,
): Promise<ServiceJob> {
  return apiRequest<ServiceJob>(
    `/api/tenants/${tenantId}/jobs/${jobId}/assign-technician`,
    {
      method: 'POST',
      body: JSON.stringify({
        technicianId,
      }),
    },
  )
}

export interface ScheduleJobRequest {
  startUtc: string
  endUtc: string
}

export async function scheduleJob(
  tenantId: string,
  jobId: string,
  request: ScheduleJobRequest,
): Promise<ServiceJob> {
  return apiRequest<ServiceJob>(
    `/api/tenants/${tenantId}/jobs/${jobId}/schedule`,
    {
      method: 'POST',
      body: JSON.stringify(request),
    },
  )
}

export async function quoteJob(
  tenantId: string,
  jobId: string,
): Promise<ServiceJob> {
  return apiRequest<ServiceJob>(
    `/api/tenants/${tenantId}/jobs/${jobId}/quote`,
    {
      method: 'POST',
    },
  )
}

export async function approveJob(
  tenantId: string,
  jobId: string,
): Promise<ServiceJob> {
  return apiRequest<ServiceJob>(
    `/api/tenants/${tenantId}/jobs/${jobId}/approve`,
    {
      method: 'POST',
    },
  )
}

export async function startJob(
  tenantId: string,
  jobId: string,
): Promise<ServiceJob> {
  return apiRequest<ServiceJob>(
    `/api/tenants/${tenantId}/jobs/${jobId}/start`,
    {
      method: 'POST',
    },
  )
}

export async function completeJob(
  tenantId: string,
  jobId: string,
): Promise<ServiceJob> {
  return apiRequest<ServiceJob>(
    `/api/tenants/${tenantId}/jobs/${jobId}/complete`,
    {
      method: 'POST',
    },
  )
}

export async function cancelJob(
  tenantId: string,
  jobId: string,
): Promise<ServiceJob> {
  return apiRequest<ServiceJob>(
    `/api/tenants/${tenantId}/jobs/${jobId}/cancel`,
    {
      method: 'POST',
    },
  )
}