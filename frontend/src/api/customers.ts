import { apiRequest } from './client'
import type { Customer } from './types'

export interface CreateCustomerRequest {
  name: string
  phone: string
  email?: string | null
  addressLine?: string | null
  city?: string | null
}

export async function getCustomers(
  tenantId: string,
  search?: string,
): Promise<Customer[]> {
  const params = new URLSearchParams()

  if (search?.trim()) {
    params.set('search', search.trim())
  }

  const query = params.toString()

  return apiRequest<Customer[]>(
    `/api/tenants/${tenantId}/customers${
      query ? `?${query}` : ''
    }`,
  )
}

export async function createCustomer(
  tenantId: string,
  request: CreateCustomerRequest,
): Promise<Customer> {
  return apiRequest<Customer>(
    `/api/tenants/${tenantId}/customers`,
    {
      method: 'POST',
      body: JSON.stringify(request),
    },
  )
}