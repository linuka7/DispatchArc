export type UserRole =
  | 'Owner'
  | 'Dispatcher'
  | 'Technician'
  | 'Finance'

export type JobPriority =
  | 'Low'
  | 'Normal'
  | 'High'
  | 'Urgent'

export type JobStatus =
  | 'New'
  | 'Quoted'
  | 'Approved'
  | 'Scheduled'
  | 'InProgress'
  | 'Completed'
  | 'Invoiced'
  | 'Cancelled'

export interface AuthResponse {
  accessToken: string
  expiresAtUtc: string
  userId: string
  tenantId: string
  fullName: string
  email: string
  role: UserRole
}

export interface LoginRequest {
  tenantId: string
  email: string
  password: string
}

export interface ServiceJob {
  id: string
  tenantId: string
  customerId: string
  assignedTechnicianId: string | null
  jobNumber: string
  title: string
  description: string
  priority: JobPriority
  status: JobStatus
  scheduledStartUtc: string | null
  scheduledEndUtc: string | null
  createdAtUtc: string
  updatedAtUtc: string
}

export interface ProblemDetails {
  title?: string
  detail?: string
  status?: number
}