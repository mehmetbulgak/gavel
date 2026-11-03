import { describe, expect, it } from 'vitest'
import type { AuctionRules } from '@/features/rules/api'
import { createSellSchema, type SellInput } from './sellSchema'

const NOW = Date.UTC(2026, 10, 3, 12, 0)

const rules: AuctionRules = {
  categories: ['Electronics', 'Watches'],
  durationsInMinutes: [5, 60, 1440],
  titleMinLength: 5,
  titleMaxLength: 100,
  descriptionMaxLength: 2000,
  minStartingPrice: 1,
  maxStartingPrice: 10_000_000,
  maxBidAmount: 100_000_000,
  maxScheduleAheadMinutes: 7 * 24 * 60,
  snipeProtectionSeconds: 120,
}

const valid: SellInput = {
  title: 'Leica M6 with 50mm Summicron',
  category: 'Electronics',
  description: 'Serviced in 2025.\nComes with the box.',
  startingPrice: '15000',
  reservePrice: '',
  durationMinutes: '60',
  startMode: 'now',
  startsAt: '',
}

const schema = createSellSchema(rules, () => NOW)
const errorPaths = (input: SellInput) => schema.safeParse(input).error?.issues.map((issue) => issue.path.join('.')) ?? []
const localInput = (time: number) => new Date(time - new Date(time).getTimezoneOffset() * 60_000).toISOString().slice(0, 16)

describe('createSellSchema', () => {
  it('accepts a complete listing and converts the numbers', () => {
    const result = schema.parse(valid)

    expect(result.startingPrice).toBe(15000)
    expect(result.reservePrice).toBeUndefined()
    expect(result.durationMinutes).toBe(60)
  })

  it('requires a category and a title of sensible length', () => {
    expect(errorPaths({ ...valid, title: 'Cam', category: '' })).toEqual(['title', 'category'])
  })

  it('rejects control characters', () => {
    expect(errorPaths({ ...valid, title: 'Leica\u0000M6', description: 'Fine\u0007' })).toEqual(['title', 'description'])
  })

  it('rejects a reserve below the starting price', () => {
    expect(errorPaths({ ...valid, reservePrice: '9000' })).toEqual(['reservePrice'])
  })

  it('only offers the durations the server allows', () => {
    expect(errorPaths({ ...valid, durationMinutes: '7' })).toEqual(['durationMinutes'])
  })

  it.each([
    ['', 'missing'],
    [localInput(NOW - 60_000), 'in the past'],
    [localInput(NOW + 8 * 24 * 3_600_000), 'too far ahead'],
  ])('rejects a scheduled start that is %s (%s)', (startsAt) => {
    expect(errorPaths({ ...valid, startMode: 'later', startsAt })).toEqual(['startsAt'])
  })

  it('accepts a scheduled start inside the window', () => {
    expect(errorPaths({ ...valid, startMode: 'later', startsAt: localInput(NOW + 3_600_000) })).toEqual([])
  })
})
