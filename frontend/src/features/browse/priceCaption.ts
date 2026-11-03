import type { AuctionSummary } from '@/lib/api/types'

export function priceCaption(auction: Pick<AuctionSummary, 'status' | 'bidCount' | 'outcome'>) {
  if (auction.status === 'Ended') return auction.outcome === 'Sold' ? 'Sold for' : 'Ended without a sale'
  if (auction.status === 'Cancelled') return 'Withdrawn'
  return auction.bidCount === 0 ? 'Starting bid' : 'Current bid'
}
