import { describe, expect, it } from 'vitest'
import type { Bid } from '@/lib/api/types'
import { buildPriceSeries, pointAt } from './priceSeries'

const bid = (sequence: number, amount: number, minute: number): Bid => ({
  id: `b${sequence}`,
  alias: `Bidder ${sequence}`,
  amount,
  isAutomatic: false,
  sequence,
  placedAt: new Date(Date.UTC(2026, 10, 3, 12, minute)).toISOString(),
  isMine: false,
})

const startsAt = new Date(Date.UTC(2026, 10, 3, 12, 0)).toISOString()

describe('buildPriceSeries', () => {
  it('starts at the starting price and never moves down', () => {
    const points = buildPriceSeries({
      startingPrice: 100,
      startsAt,
      bids: [bid(3, 180, 6), bid(1, 100, 2), bid(2, 170, 5)],
      complete: true,
    })

    expect(points.map((p) => p.price)).toEqual([100, 100, 170, 180])
    expect(points[0]?.bid).toBeNull()
  })

  it('skips the opening point when older bids are missing', () => {
    const points = buildPriceSeries({ startingPrice: 100, startsAt, bids: [bid(40, 900, 9)], complete: false })

    expect(points).toHaveLength(1)
    expect(points[0]?.price).toBe(900)
  })
})

describe('pointAt', () => {
  it('returns the step that was in force at a moment', () => {
    const points = buildPriceSeries({ startingPrice: 100, startsAt, bids: [bid(1, 120, 2), bid(2, 150, 5)], complete: true })

    expect(pointAt(points, Date.UTC(2026, 10, 3, 12, 1))).toBe(0)
    expect(pointAt(points, Date.UTC(2026, 10, 3, 12, 4))).toBe(1)
    expect(pointAt(points, Date.UTC(2026, 10, 3, 13, 0))).toBe(2)
  })
})
