import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { setSession } from '@/lib/api/session'
import type { AuctionDetails } from '@/lib/api/types'
import { BidForm } from './BidForm'

const auction = {
  id: '0192f0c4-3a7e-7d2a-9b1e-5f6a7b8c9d0e',
  status: 'Live',
  currentPrice: 300,
  minimumNextBid: 310,
  increment: 10,
  viewer: { isSeller: false, isWatching: false, isLeading: false, alias: null, maxBid: null, isWinner: false },
} as AuctionDetails

const fetchMock = vi.fn<typeof fetch>()

const renderForm = (details: AuctionDetails = auction) =>
  render(
    <QueryClientProvider client={new QueryClient()}>
      <BidForm auction={details} />
    </QueryClientProvider>,
  )

describe('BidForm', () => {
  beforeEach(() => {
    vi.stubGlobal('fetch', fetchMock)
    setSession({ accessToken: 'token', expiresAt: '2026-11-03T13:00:00Z', user: { id: 'u1', email: 'a@b.c', displayName: 'A' } })
  })

  afterEach(() => {
    fetchMock.mockReset()
    vi.unstubAllGlobals()
    setSession(null)
  })

  it('stops a bid below the minimum before it reaches the server', async () => {
    renderForm()

    await userEvent.type(screen.getByLabelText('Your maximum bid'), '305')
    await userEvent.click(screen.getByRole('button', { name: 'Bid' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Enter at least')
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it('fills the amount from a quick bid and sends it', async () => {
    fetchMock.mockResolvedValueOnce(
      new Response(
        JSON.stringify({
          isLeading: true,
          alias: 'Bidder 4',
          yourMaxBid: 330,
          currentPrice: 310,
          minimumNextBid: 320,
          bidCount: 5,
          endsAt: '2026-11-03T13:00:00Z',
          extended: false,
        }),
        { status: 200, headers: { 'Content-Type': 'application/json' } },
      ),
    )
    renderForm()

    await userEvent.click(screen.getAllByRole('button', { name: /330/ })[0]!)
    await userEvent.click(screen.getByRole('button', { name: 'Bid' }))

    expect(fetchMock).toHaveBeenCalledOnce()
    const [url, init] = fetchMock.mock.calls[0]!
    expect(url).toBe(`/api/auctions/${auction.id}/bids`)
    expect(JSON.parse(init?.body as string)).toEqual({ maxAmount: 330 })
  })

  it('asks a leader to raise above their current maximum', async () => {
    renderForm({ ...auction, viewer: { ...auction.viewer!, isLeading: true, alias: 'Bidder 4', maxBid: 500 } })

    await userEvent.type(screen.getByLabelText('Raise your maximum'), '450')
    await userEvent.click(screen.getByRole('button', { name: 'Bid' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('You already lead with a maximum of')
    expect(fetchMock).not.toHaveBeenCalled()
  })
})
