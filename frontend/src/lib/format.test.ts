import { describe, expect, it } from 'vitest'
import { formatRemaining, paddleNumber, pluralize } from './format'

describe('formatRemaining', () => {
  it.each([
    [0, 'Ended'],
    [-5, 'Ended'],
    [9_400, '9s'],
    [125_000, '2m 05s'],
    [3_725_000, '1h 2m'],
    [2 * 86_400_000 + 3_600_000, '2d 1h'],
  ])('formats %d ms as %s', (ms, expected) => {
    expect(formatRemaining(ms)).toBe(expected)
  })
})

describe('paddleNumber', () => {
  it('reads the number at the end of an alias', () => {
    expect(paddleNumber('Bidder 12')).toBe(12)
    expect(paddleNumber('Bidder')).toBeNull()
    expect(paddleNumber(null)).toBeNull()
  })
})

describe('pluralize', () => {
  it('picks the singular only for exactly one', () => {
    expect(pluralize(1, 'bid')).toBe('1 bid')
    expect(pluralize(0, 'bid')).toBe('0 bids')
    expect(pluralize(1200, 'bid')).toBe('1,200 bids')
  })
})
