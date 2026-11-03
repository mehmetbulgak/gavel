import { QueryClient } from '@tanstack/react-query'
import { describe, expect, it } from 'vitest'
import type { AuctionDetails, AuctionSummary, AuctionUpdate, Bid, Paged } from '../api/types'
import { queryKeys } from '../query/keys'
import { applyAuctionUpdate } from './applyAuctionUpdate'

const AUCTION_ID = '0192f0c4-3a7e-7d2a-9b1e-5f6a7b8c9d0e'

const bid = (sequence: number, alias: string, amount: number): Bid => ({
  id: `bid-${sequence}`,
  alias,
  amount,
  isAutomatic: false,
  sequence,
  placedAt: '2026-11-03T12:00:00Z',
  isMine: false,
})

const details = {
  id: AUCTION_ID,
  status: 'Live',
  currentPrice: 100,
  minimumNextBid: 105,
  bidCount: 1,
  bidderCount: 1,
  endsAt: '2026-11-03T13:00:00Z',
  leaderAlias: 'Bidder 1',
  reserveMet: false,
  outcome: null,
  recentBids: [bid(1, 'Bidder 1', 100)],
  viewer: { isSeller: false, isWatching: false, isLeading: true, alias: 'Bidder 1', maxBid: 150, isWinner: false },
} as AuctionDetails

const update = (overrides: Partial<AuctionUpdate> = {}): AuctionUpdate => ({
  auctionId: AUCTION_ID,
  status: 'Live',
  currentPrice: 155,
  minimumNextBid: 160,
  bidCount: 3,
  bidderCount: 2,
  endsAt: '2026-11-03T13:02:00Z',
  leaderAlias: 'Bidder 2',
  reserveMet: true,
  outcome: null,
  newBids: [bid(3, 'Bidder 2', 155), bid(2, 'Bidder 1', 150)],
  ...overrides,
})

function seeded() {
  const client = new QueryClient()
  client.setQueryData(queryKeys.auction(AUCTION_ID), details)
  client.setQueryData<Paged<Bid>>(queryKeys.bids(AUCTION_ID), {
    items: [bid(1, 'Bidder 1', 100)],
    page: 1,
    pageSize: 100,
    totalCount: 1,
    totalPages: 1,
  })
  client.setQueryData<Paged<AuctionSummary>>(queryKeys.auctionList({ status: 'Live' }), {
    items: [{ id: AUCTION_ID, currentPrice: 100, bidCount: 1 } as AuctionSummary],
    page: 1,
    pageSize: 12,
    totalCount: 1,
    totalPages: 1,
  })
  return client
}

describe('applyAuctionUpdate', () => {
  it('moves the price and tells the viewer they were outbid', () => {
    const client = seeded()

    applyAuctionUpdate(client, update())

    const auction = client.getQueryData<AuctionDetails>(queryKeys.auction(AUCTION_ID))
    expect(auction?.currentPrice).toBe(155)
    expect(auction?.viewer?.isLeading).toBe(false)
    expect(auction?.recentBids.map((b) => b.sequence)).toEqual([3, 2, 1])
    expect(auction?.recentBids.find((b) => b.sequence === 2)?.isMine).toBe(true)
  })

  it('adds new bids to the history once, even when an update repeats', () => {
    const client = seeded()

    applyAuctionUpdate(client, update())
    applyAuctionUpdate(client, update())

    const history = client.getQueryData<Paged<Bid>>(queryKeys.bids(AUCTION_ID))
    expect(history?.items.map((b) => b.id)).toEqual(['bid-3', 'bid-2', 'bid-1'])
    expect(history?.totalCount).toBe(3)
  })

  it('updates the auction wherever it appears in a list', () => {
    const client = seeded()

    applyAuctionUpdate(client, update())

    const list = client.getQueryData<Paged<AuctionSummary>>(queryKeys.auctionList({ status: 'Live' }))
    expect(list?.items[0]).toMatchObject({ currentPrice: 155, bidCount: 3, reserveMet: true })
  })
})
