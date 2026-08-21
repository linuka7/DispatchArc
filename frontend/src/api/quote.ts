import { apiRequest } from './client'

export interface JobLineItemResponse {
  id: string
  tenantId: string
  serviceJobId: string
  description: string
  quantity: number
  unitPrice: number
  lineTotal: number
  createdAtUtc: string
  updatedAtUtc: string
}

export interface JobQuoteResponse {
  tenantId: string
  serviceJobId: string
  lineItems: JobLineItemResponse[]
  subtotal: number
}

export interface SaveJobLineItemRequest {
  description: string
  quantity: number
  unitPrice: number
}

export async function getJobQuote(
  tenantId: string,
  jobId: string,
): Promise<JobQuoteResponse> {
  return apiRequest<JobQuoteResponse>(
    `/api/tenants/${tenantId}/jobs/${jobId}/quote`,
  )
}

export async function addJobLineItem(
  tenantId: string,
  jobId: string,
  request: SaveJobLineItemRequest,
): Promise<JobLineItemResponse> {
  return apiRequest<JobLineItemResponse>(
    `/api/tenants/${tenantId}/jobs/${jobId}/quote/line-items`,
    {
      method: 'POST',
      body: JSON.stringify(request),
    },
  )
}

export async function updateJobLineItem(
  tenantId: string,
  jobId: string,
  lineItemId: string,
  request: SaveJobLineItemRequest,
): Promise<JobLineItemResponse> {
  return apiRequest<JobLineItemResponse>(
    `/api/tenants/${tenantId}/jobs/${jobId}/quote/line-items/${lineItemId}`,
    {
      method: 'PUT',
      body: JSON.stringify(request),
    },
  )
}

export async function deleteJobLineItem(
  tenantId: string,
  jobId: string,
  lineItemId: string,
): Promise<void> {
  return apiRequest<void>(
    `/api/tenants/${tenantId}/jobs/${jobId}/quote/line-items/${lineItemId}`,
    {
      method: 'DELETE',
    },
  )
}
