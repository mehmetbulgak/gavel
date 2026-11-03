import { describe, expect, it } from 'vitest'
import { bidSchema, quickBids } from './bidSchema'

const parse = (maxAmount: string) => bidSchema.safeParse({ maxAmount })

describe('bidSchema', () => {
  it.each(['150', '1234.57', '0.1'])('accepts %s', (amount) => {
    expect(parse(amount).success).toBe(true)
  })

  it.each(['', '0', '-5', '12.345', '100000000.01'])('rejects %s', (amount) => {
    expect(parse(amount).success).toBe(false)
  })

  it('turns the text into a number', () => {
    expect(parse('1234.5').data).toEqual({ maxAmount: 1234.5 })
  })
})

describe('quickBids', () => {
  it('offers the minimum and two bigger jumps', () => {
    expect(quickBids(320, 10)).toEqual([320, 340, 370])
  })
})
