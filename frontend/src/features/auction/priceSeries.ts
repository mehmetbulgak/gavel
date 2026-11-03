import type { Bid } from '@/lib/api/types'

export type PricePoint = { time: number; price: number; bid: Bid | null }

type SeriesInput = { startingPrice: number; startsAt: string; bids: readonly Bid[]; complete: boolean }

export function buildPriceSeries({ startingPrice, startsAt, bids, complete }: SeriesInput): PricePoint[] {
  const ordered = [...bids].sort((a, b) => a.sequence - b.sequence)
  const points: PricePoint[] = complete ? [{ time: Date.parse(startsAt), price: startingPrice, bid: null }] : []
  let price = complete ? startingPrice : 0

  for (const bid of ordered) {
    price = Math.max(price, bid.amount)
    points.push({ time: Date.parse(bid.placedAt), price, bid })
  }

  return points
}

export function pointAt(points: readonly PricePoint[], time: number) {
  let index = 0
  for (let i = 0; i < points.length; i++) {
    const point = points[i]
    if (point && point.time <= time) index = i
  }
  return index
}
