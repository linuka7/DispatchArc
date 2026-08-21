import { apiRequest } from './client'
import type { UserRole } from './types'

export interface Technician {
  id: string
  fullName: string
  email: string
}

export interface TeamMember {
  id: string
  tenantId: string
  fullName: string
  email: string
  role: UserRole
  isActive: boolean
  createdAtUtc: string
  updatedAtUtc: string
}

export interface CreateTechnicianRequest {
  fullName: string
  email: string
  password: string
  role: 'Technician'
}

/**
 * Get active technicians available for job assignment.
 */
export async function getTechnicians(
  tenantId: string,
): Promise<Technician[]> {
  return apiRequest<Technician[]>(
    `/api/tenants/${tenantId}/technicians`,
  )
}

/**
 * Get all team members for the workspace.
 */
export async function getTeamMembers(
  tenantId: string,
): Promise<TeamMember[]> {
  return apiRequest<TeamMember[]>(
    `/api/tenants/${tenantId}/team-members`,
  )
}

/**
 * Create a technician/team member.
 */
export async function createTechnician(
  tenantId: string,
  request: CreateTechnicianRequest,
): Promise<TeamMember> {
  return apiRequest<TeamMember>(
    `/api/tenants/${tenantId}/team-members`,
    {
      method: 'POST',
      body: JSON.stringify(request),
    },
  )
}