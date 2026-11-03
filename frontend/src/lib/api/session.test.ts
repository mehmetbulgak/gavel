import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { getAccessToken, getFreshAccessToken, onSessionChange, refreshSession, setSession } from './session'
import type { AuthResponse } from './types'

const session: AuthResponse = {
  accessToken: 'fresh-token',
  expiresAt: '2026-11-03T10:00:00Z',
  user: { id: 'u1', email: 'a@b.c', displayName: 'A' },
}

const json = (status: number, body: unknown) =>
  new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/problem+json' } })

const fetchMock = vi.fn<typeof fetch>()

describe('refreshSession', () => {
  beforeEach(() => {
    vi.stubGlobal('fetch', fetchMock)
    setSession(null)
  })

  afterEach(() => {
    fetchMock.mockReset()
    vi.unstubAllGlobals()
  })

  it('retries once when another tab rotated the token a moment earlier', async () => {
    fetchMock.mockResolvedValueOnce(json(401, { code: 'Auth.RefreshTokenRotated' })).mockResolvedValueOnce(json(200, session))

    await expect(refreshSession()).resolves.toEqual(session)

    expect(fetchMock).toHaveBeenCalledTimes(2)
    expect(getAccessToken()).toBe('fresh-token')
  })

  it('gives up without retrying when the session is invalid', async () => {
    fetchMock.mockResolvedValueOnce(json(401, { code: 'Auth.InvalidRefreshToken' }))

    await expect(refreshSession()).resolves.toBeNull()

    expect(fetchMock).toHaveBeenCalledTimes(1)
    expect(getAccessToken()).toBeNull()
  })

  it('shares one request between concurrent callers', async () => {
    fetchMock.mockResolvedValueOnce(json(200, session))

    const [first, second] = await Promise.all([refreshSession(), refreshSession()])

    expect(first).toEqual(session)
    expect(second).toEqual(session)
    expect(fetchMock).toHaveBeenCalledTimes(1)
  })
})

describe('session resilience', () => {
  beforeEach(() => {
    vi.stubGlobal('fetch', fetchMock)
    setSession(null)
  })

  afterEach(() => {
    fetchMock.mockReset()
    vi.unstubAllGlobals()
    vi.useRealTimers()
  })

  it('keeps the signed-in user when a refresh is rate limited', async () => {
    setSession(session)
    fetchMock.mockResolvedValueOnce(json(429, { title: 'Too many requests' }))

    await expect(refreshSession()).resolves.toBeNull()

    expect(getAccessToken()).toBe('fresh-token')
  })

  it('treats a failed first refresh as signed out', async () => {
    const changes: unknown[] = []
    const unsubscribe = onSessionChange((value) => changes.push(value))
    fetchMock.mockResolvedValueOnce(json(503, {}))

    await refreshSession()
    unsubscribe()

    expect(changes).toEqual([null])
  })

  it('refreshes before handing out a token that is about to expire', async () => {
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(new Date('2026-11-03T09:59:30Z'))
    setSession(session)
    fetchMock.mockResolvedValueOnce(json(200, { ...session, accessToken: 'next-token', expiresAt: '2026-11-03T10:15:00Z' }))

    await expect(getFreshAccessToken()).resolves.toBe('next-token')
    await expect(getFreshAccessToken()).resolves.toBe('next-token')
    expect(fetchMock).toHaveBeenCalledOnce()
  })
})
