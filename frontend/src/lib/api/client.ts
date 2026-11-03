import { toApiError } from './errors'
import { getAccessToken, refreshSession } from './session'

type QueryValue = string | number | boolean | null | undefined

export type Query = Record<string, QueryValue>

type RequestOptions = {
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE'
  body?: unknown
  query?: Query
  signal?: AbortSignal
}

const AUTH_PREFIX = '/api/auth/'

export async function api<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const url = buildUrl(path, options.query)
  let response = await send(url, options)

  if (response.status === 401 && !path.startsWith(AUTH_PREFIX)) {
    const session = await refreshSession()
    if (session) {
      response = await send(url, options)
    }
  }

  if (!response.ok) {
    throw await toApiError(response)
  }

  if (response.status === 204) {
    return undefined as T
  }

  return (await response.json()) as T
}

function send(url: string, { method = 'GET', body, signal }: RequestOptions) {
  const headers: Record<string, string> = { Accept: 'application/json' }
  const token = getAccessToken()
  if (body !== undefined) headers['Content-Type'] = 'application/json'
  if (token) headers.Authorization = `Bearer ${token}`

  return fetch(url, {
    method,
    headers,
    body: body === undefined ? undefined : JSON.stringify(body),
    credentials: 'same-origin',
    signal,
  })
}

function buildUrl(path: string, query?: Query) {
  if (!query) return path

  const params = new URLSearchParams()
  for (const [key, value] of Object.entries(query)) {
    if (value !== undefined && value !== null && value !== '') params.set(key, String(value))
  }

  const queryString = params.toString()
  return queryString ? `${path}?${queryString}` : path
}
