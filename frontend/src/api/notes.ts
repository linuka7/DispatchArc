import { apiRequest } from './client'

export type JobNoteType = 'InternalNote' | 'TechnicianUpdate'

export interface JobNoteResponse {
  id: string
  tenantId: string
  serviceJobId: string
  authorUserId: string
  authorFullName: string
  type: JobNoteType
  content: string
  createdAtUtc: string
}

export interface AddJobNoteRequest {
  type: JobNoteType
  content: string
}

export async function getJobNotes(
  tenantId: string,
  jobId: string,
): Promise<JobNoteResponse[]> {
  return apiRequest<JobNoteResponse[]>(
    `/api/tenants/${tenantId}/jobs/${jobId}/notes`,
  )
}

export async function addJobNote(
  tenantId: string,
  jobId: string,
  request: AddJobNoteRequest,
): Promise<JobNoteResponse> {
  return apiRequest<JobNoteResponse>(
    `/api/tenants/${tenantId}/jobs/${jobId}/notes`,
    {
      method: 'POST',
      body: JSON.stringify(request),
    },
  )
}
