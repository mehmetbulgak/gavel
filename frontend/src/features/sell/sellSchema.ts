import { z } from 'zod'
import { formatDuration } from '@/features/rules/formatDuration'
import type { AuctionRules } from '@/features/rules/api'
import { AUCTION_CATEGORIES } from '@/lib/api/types'
import { formatMoney } from '@/lib/format'
import { hasControlCharacters } from '@/lib/text'

const MINUTE_MS = 60_000

const amount = (min: number, max: number) =>
  z.coerce
    .number<string>({ error: 'Enter a number' })
    .min(min, `Enter at least ${formatMoney(min)}`)
    .max(max, `Enter at most ${formatMoney(max)}`)
    .multipleOf(0.01, 'Use at most two decimals')

export const START_MODES = ['now', 'later'] as const

export function createSellSchema(rules: AuctionRules, now: () => number = Date.now) {
  return z
    .object({
      title: z
        .string()
        .trim()
        .min(rules.titleMinLength, `Use at least ${rules.titleMinLength} characters`)
        .max(rules.titleMaxLength, `Use at most ${rules.titleMaxLength} characters`)
        .refine((value) => !hasControlCharacters(value), 'Remove special characters from the title'),
      category: z.string().pipe(z.enum(AUCTION_CATEGORIES, { error: 'Choose a category' })),
      description: z
        .string()
        .trim()
        .max(rules.descriptionMaxLength, `Use at most ${rules.descriptionMaxLength} characters`)
        .refine((value) => !hasControlCharacters(value, { allowLineBreaks: true }), 'Remove special characters from the description'),
      startingPrice: z.string().trim().min(1, 'Enter a starting price').pipe(amount(rules.minStartingPrice, rules.maxStartingPrice)),
      reservePrice: z
        .string()
        .trim()
        .transform((value) => (value === '' ? undefined : value))
        .pipe(amount(rules.minStartingPrice, rules.maxBidAmount).optional()),
      durationMinutes: z
        .string()
        .transform(Number)
        .refine((value) => rules.durationsInMinutes.includes(value), 'Choose how long the auction runs'),
      startMode: z.enum(START_MODES),
      startsAt: z.string(),
    })
    .superRefine((values, ctx) => {
      if (values.reservePrice !== undefined && values.reservePrice < values.startingPrice) {
        ctx.addIssue({ code: 'custom', path: ['reservePrice'], message: 'The reserve cannot be lower than the starting price' })
      }

      if (values.startMode !== 'later') return
      const start = Date.parse(values.startsAt)
      const current = now()
      if (Number.isNaN(start)) {
        ctx.addIssue({ code: 'custom', path: ['startsAt'], message: 'Pick a date and time' })
      } else if (start <= current + MINUTE_MS) {
        ctx.addIssue({ code: 'custom', path: ['startsAt'], message: 'Pick a time at least a minute from now' })
      } else if (start - current > rules.maxScheduleAheadMinutes * MINUTE_MS) {
        ctx.addIssue({
          code: 'custom',
          path: ['startsAt'],
          message: `Pick a time within the next ${formatDuration(rules.maxScheduleAheadMinutes)}`,
        })
      }
    })
}

export type SellInput = z.input<ReturnType<typeof createSellSchema>>
export type SellValues = z.output<ReturnType<typeof createSellSchema>>
