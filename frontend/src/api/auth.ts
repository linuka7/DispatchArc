import { apiRequest, sessionKey } from './client'
import type {
  AuthResponse,
  LoginRequest,
} from './types'
import { demoTenantId } from './demo'

export function startDemoSession(): AuthResponse {
  const response: AuthResponse = {
    accessToken: 'demo-token',
    expiresAtUtc: new Date(Date.now() + 86400000).toISOString(),
    userId: 'demo-owner',
    tenantId: demoTenantId,
    fullName: 'ARK II',
    email: 'owner@dispatcharc.demo',
    role: 'Owner',
  }

  sessionStorage.setItem(sessionKey, JSON.stringify(response))
  return response
}

export async function login(
  request: LoginRequest,
): Promise<AuthResponse> {
  const response = await apiRequest<AuthResponse>(
    '/api/auth/login',
    {
      method: 'POST',
      body: JSON.stringify(request),
    },
  )

  sessionStorage.setItem(
    sessionKey,
    JSON.stringify(response),
  )

  return response
}

export function logout(): void {
  sessionStorage.removeItem(sessionKey)
}

export function getCurrentSession():
  | AuthResponse
  | null {
  const raw = sessionStorage.getItem(sessionKey)

  if (!raw) {
    return null
  }

  try {
    return JSON.parse(raw) as AuthResponse
  } catch {
    sessionStorage.removeItem(sessionKey)
    return null
  }
}