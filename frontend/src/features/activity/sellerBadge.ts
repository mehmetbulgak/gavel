import type { AuctionSummary } from '@/lib/api/types'

type Tone = 'good' | 'bad' | 'neutral' | 'info'

export function sellerBadge(auction: AuctionSummary): { tone: Tone; label: string } {
  switch (auction.status) {
    case 'Scheduled':
      return { tone: 'info', label: 'Scheduled' }
    case 'Live':
      return auction.hasReserve && !auction.reserveMet && auction.bidCount > 0
        ? { tone: 'neutral', label: 'Below reserve' }
        : { tone: 'info', label: 'Live' }
    case 'Cancelled':
      return { tone: 'neutral', label: 'Withdrawn' }
    case 'Ended':
      if (auction.outcome === 'Sold') return { tone: 'good', label: 'Sold' }
      return { tone: 'bad', label: auction.outcome === 'ReserveNotMet' ? 'Reserve not met' : 'No bids' }
  }
}
