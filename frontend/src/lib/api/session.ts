import { toApiError } from './errors'
import type { AuthResponse } from './types'

type SessionListener = (session: AuthResponse | null) => void

const REFRESH_URL = '/api/auth/refresh'
const ROTATED_ELSEWHERE = 'Auth.RefreshTokenRotated'
const ROTATION_RETRY_DELAY_MS = 250
const EXPIRY_MARGIN_MS = 60_000
const SIGNED_OUT_STATUSES = new Set([400, 401, 403])

class TransientRefreshError extends Error {}

let accessToken: string | null = null
let expiresAt = 0
let refreshInFlight: Promise<AuthResponse | null> | null = null
const listeners = new Set<SessionListener>()

export function getAccessToken() {
  return accessToken
}

export async function getFreshAccessToken() {
  if (accessToken && expiresAt - Date.now() > EXPIRY_MARGIN_MS) return accessToken
  return (await refreshSession())?.accessToken ?? null
}

export function setSession(session: AuthResponse | null) {
  accessToken = session?.accessToken ?? null
  expiresAt = session ? Date.parse(session.expiresAt) : 0
  listeners.forEach((listener) => listener(session))
}

export function onSessionChange(listener: SessionListener) {
  listeners.add(listener)
  return () => {
    listeners.delete(listener)
  }
}

export function refreshSession(): Promise<AuthResponse | null> {
  refreshInFlight ??= requestRefresh()
    .then((session) => {
      setSession(session)
      return session
    })
    .catch(() => {
      if (!accessToken) setSession(null)
      return null
    })
    .finally(() => {
      refreshInFlight = null
    })
  return refreshInFlight
}

async function requestRefresh(retryIfRotatedElsewhere = true): Promise<AuthResponse | null> {
  const response = await fetch(REFRESH_URL, { method: 'POST', credentials: 'same-origin' })
  if (response.ok) {
    return (await response.json()) as AuthResponse
  }

  if (!SIGNED_OUT_STATUSES.has(response.status)) {
    throw new TransientRefreshError(`Refreshing the session failed with ${response.status}`)
  }

  if (retryIfRotatedElsewhere && (await toApiError(response)).code === ROTATED_ELSEWHERE) {
    await new Promise((resolve) => setTimeout(resolve, ROTATION_RETRY_DELAY_MS))
    return requestRefresh(false)
  }

  return null
}
