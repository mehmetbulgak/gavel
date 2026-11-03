import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { api } from './client'
import { ApiError } from './errors'
import { refreshSession, setSession } from './session'
import type { AuthResponse } from './types'

const session = (token: string): AuthResponse => ({
  accessToken: token,
  expiresAt: '2026-11-03T10:00:00Z',
  user: { id: 'u1', email: 'a@b.c', displayName: 'A' },
})

const json = (status: number, body: unknown) =>
  new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } })

const headersOf = (call: number) => fetchMock.mock.calls[call]![1]!.headers as Record<string, string>

const fetchMock = vi.fn<typeof fetch>()

describe('api client', () => {
  beforeEach(() => {
    vi.stubGlobal('fetch', fetchMock)
    setSession(session('old-token'))
  })

  afterEach(() => {
    fetchMock.mockReset()
    vi.unstubAllGlobals()
  })

  it('sends the access token as a bearer header', async () => {
    fetchMock.mockResolvedValueOnce(json(200, { ok: true }))

    await api('/api/me/bids')

    expect(headersOf(0).Authorization).toBe('Bearer old-token')
  })

  it('refreshes once on 401 and replays the request with the new token', async () => {
    fetchMock
      .mockResolvedValueOnce(json(401, {}))
      .mockResolvedValueOnce(json(200, session('new-token')))
      .mockResolvedValueOnce(json(200, { items: [] }))

    const result = await api<{ items: unknown[] }>('/api/me/bids')

    expect(result.items).toEqual([])
    expect(fetchMock.mock.calls[1]![0]).toBe('/api/auth/refresh')
    expect(headersOf(2).Authorization).toBe('Bearer new-token')
  })

  it('does not try to refresh after a failing auth call', async () => {
    fetchMock.mockResolvedValueOnce(json(401, { detail: 'Email or password is incorrect.' }))

    await expect(api('/api/auth/login', { method: 'POST', body: {} })).rejects.toBeInstanceOf(ApiError)
    expect(fetchMock).toHaveBeenCalledTimes(1)
  })

  it('shares a single refresh request between concurrent callers', async () => {
    fetchMock.mockResolvedValue(json(200, session('fresh')))

    await Promise.all([refreshSession(), refreshSession(), refreshSession()])

    expect(fetchMock).toHaveBeenCalledTimes(1)
  })

  it('turns problem responses into ApiError with the stable code', async () => {
    fetchMock.mockResolvedValueOnce(
      json(409, { title: 'Conflict', detail: 'Not enough stock for CASE-01', code: 'Product.InsufficientStock' }),
    )

    const error = await api('/api/orders', { method: 'POST', body: {} }).catch((e: unknown) => e)

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ status: 409, code: 'Product.InsufficientStock', message: 'Not enough stock for CASE-01' })
  })

  it('surfaces the first field error of a validation problem', async () => {
    fetchMock.mockResolvedValueOnce(json(400, { title: 'Validation failed', errors: { Sku: ['SKU is required.'] } }))

    await expect(api('/api/me/bids', { method: 'POST', body: {} })).rejects.toMatchObject({
      message: 'SKU is required.',
      fieldErrors: { Sku: ['SKU is required.'] },
    })
  })

  it('explains rate limiting in plain words', async () => {
    fetchMock.mockResolvedValueOnce(json(429, { title: 'Too Many Requests', status: 429 }))

    await expect(api('/api/auth/login', { method: 'POST', body: {} })).rejects.toMatchObject({
      status: 429,
      message: 'Too many attempts. Please wait a minute and try again.',
    })
  })

  it('keeps the server explanation of an account-level sign-in limit', async () => {
    const detail = 'Too many failed sign-in attempts for this account. Please wait a few minutes and try again.'
    fetchMock.mockResolvedValueOnce(json(429, { detail, code: 'Auth.TooManyFailedSignIns' }))

    await expect(api('/api/auth/login', { method: 'POST', body: {} })).rejects.toMatchObject({
      status: 429,
      code: 'Auth.TooManyFailedSignIns',
      message: detail,
    })
  })

  it('drops empty query values', async () => {
    fetchMock.mockResolvedValueOnce(json(200, {}))

    await api('/api/me/bids', { query: { search: '', lowStockOnly: true, page: 2, sort: undefined } })

    expect(fetchMock.mock.calls[0]![0]).toBe('/api/me/bids?lowStockOnly=true&page=2')
  })
})
