import type { AuctionDetails } from '@/lib/api/types'

export const isOpen = (auction: Pick<AuctionDetails, 'status'>) => auction.status === 'Live' || auction.status === 'Scheduled'

export function priceLabel(auction: Pick<AuctionDetails, 'status' | 'bidCount' | 'outcome'>) {
  if (auction.status === 'Ended') {
    if (auction.outcome === 'Sold') return 'Sold for'
    return auction.bidCount > 0 ? 'Highest bid' : 'Starting bid'
  }
  if (auction.status === 'Cancelled') return 'Starting bid'
  return auction.bidCount === 0 ? 'Starting bid' : 'Current bid'
}

export type ViewerStanding =
  { kind: 'seller' } | { kind: 'leading'; maxBid: number } | { kind: 'outbid' } | { kind: 'won' } | { kind: 'lost' } | null

export function viewerStanding(auction: AuctionDetails): ViewerStanding {
  const viewer = auction.viewer
  if (!viewer) return null
  if (viewer.isSeller) return { kind: 'seller' }
  if (!viewer.alias) return null
  if (auction.status === 'Ended') return viewer.isWinner ? { kind: 'won' } : { kind: 'lost' }
  if (auction.status !== 'Live') return null
  return viewer.isLeading && viewer.maxBid !== null ? { kind: 'leading', maxBid: viewer.maxBid } : { kind: 'outbid' }
}
