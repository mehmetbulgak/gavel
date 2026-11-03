import { z } from 'zod'

export const MAX_BID = 100_000_000

export const bidSchema = z.object({
  maxAmount: z.coerce
    .number<string>({ error: 'Enter an amount' })
    .positive('Enter an amount')
    .max(MAX_BID, 'That is above the 100,000,000 limit')
    .multipleOf(0.01, 'Use at most two decimals'),
})

export type BidInput = z.input<typeof bidSchema>
export type BidValues = z.output<typeof bidSchema>

export function quickBids(minimum: number, increment: number) {
  return [minimum, minimum + increment * 2, minimum + increment * 5]
}
