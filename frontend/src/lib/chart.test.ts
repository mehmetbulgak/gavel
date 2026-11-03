import { describe, expect, it } from 'vitest'
import { niceTicks } from './chart'

describe('niceTicks', () => {
  it('covers the range with round steps', () => {
    const ticks = niceTicks(1200, 4870)

    expect(ticks[0]).toBeLessThanOrEqual(1200)
    expect(ticks.at(-1)).toBeGreaterThanOrEqual(4870)
    expect(new Set(ticks.slice(1).map((tick, i) => tick - (ticks[i] ?? 0))).size).toBe(1)
    expect(ticks).toContain(2000)
  })

  it('still produces a range when every value is equal', () => {
    const ticks = niceTicks(500, 500)

    expect(ticks.length).toBeGreaterThan(1)
    expect(ticks.at(-1)).toBeGreaterThanOrEqual(500)
  })
})
