import type { ProblemDetails } from './types'
import { demoRequest } from './demo'

const apiBaseUrl =
  (import.meta.env.VITE_API_BASE_URL ??
    'http://localhost:5006').replace(/\/$/, '')

const sessionKey = 'dispatcharc.auth'

export class ApiError extends Error {
  status: number
  problem?: ProblemDetails

  constructor(
    status: number,
    message: string,
    problem?: ProblemDetails,
  ) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.problem = problem
  }
}

export function getAccessToken(): string | null {
  const raw = sessionStorage.getItem(sessionKey)

  if (!raw) {
    return null
  }

  try {
    const parsed = JSON.parse(raw) as {
      accessToken?: string
    }

    return parsed.accessToken ?? null
  } catch {
    return null
  }
}

export async function apiRequest<T>(
  path: string,
  init: RequestInit = {},
): Promise<T> {
  if (getAccessToken() === 'demo-token') {
    return demoRequest<T>(path, init)
  }

  const headers = new Headers(init.headers)

  if (init.body && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }

  const token = getAccessToken()

  if (token) {
    headers.set('Authorization', `Bearer ${token}`)
  }

  const response = await fetch(`${apiBaseUrl}${path}`, {
    ...init,
    headers,
  })

  const text = await response.text()

  let body: unknown = null

  if (text) {
    try {
      body = JSON.parse(text)
    } catch {
      body = text
    }
  }

  if (!response.ok) {
    const problem =
      typeof body === 'object' && body !== null
        ? (body as ProblemDetails)
        : undefined

    throw new ApiError(
      response.status,
      problem?.detail ??
        problem?.title ??
        `API request failed with status ${response.status}`,
      problem,
    )
  }

  return body as T
}

export { sessionKey }