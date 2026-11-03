import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { toast } from 'sonner'
import { Button } from '@/components/ui/Button'
import { Input } from '@/components/ui/FormControls'
import { describeError } from '@/lib/api/errors'
import type { AuctionDetails } from '@/lib/api/types'
import { formatMoney } from '@/lib/format'
import { usePlaceBid } from './api'
import { type BidInput, type BidValues, bidSchema, quickBids } from './bidSchema'

export function BidForm({ auction }: { auction: AuctionDetails }) {
  const placeBid = usePlaceBid(auction.id)
  const form = useForm<BidInput, unknown, BidValues>({ resolver: zodResolver(bidSchema), defaultValues: { maxAmount: '' } })
  const error = form.formState.errors.maxAmount?.message
  const isLeading = auction.viewer?.isLeading === true

  const submit = async ({ maxAmount }: BidValues) => {
    const minimum = isLeading ? (auction.viewer?.maxBid ?? 0) + 0.01 : auction.minimumNextBid
    if (maxAmount < minimum) {
      form.setError('maxAmount', {
        message: isLeading
          ? `You already lead with a maximum of ${formatMoney(auction.viewer?.maxBid ?? 0)}`
          : `Enter at least ${formatMoney(minimum)}`,
      })
      return
    }

    try {
      const result = await placeBid.mutateAsync(maxAmount)
      form.reset({ maxAmount: '' })
      if (result.isLeading) {
        toast.success('You are the highest bidder', {
          description: `We will bid for you up to ${formatMoney(result.yourMaxBid)}.${result.extended ? ' The clock was extended so others can respond.' : ''}`,
        })
      } else {
        toast.warning('Another bidder had a higher maximum', { description: `The price is now ${formatMoney(result.currentPrice)}.` })
      }
    } catch (e) {
      form.setError('maxAmount', { message: describeError(e) })
    }
  }

  return (
    <form className="grid gap-3" onSubmit={form.handleSubmit(submit)} noValidate>
      <label htmlFor="max-bid" className="text-sm text-white/80">
        {isLeading ? 'Raise your maximum' : 'Your maximum bid'}
      </label>
      <div className="flex gap-2">
        <Input
          id="max-bid"
          type="number"
          inputMode="decimal"
          step="0.01"
          min={auction.minimumNextBid}
          placeholder={formatMoney(isLeading ? (auction.viewer?.maxBid ?? auction.minimumNextBid) : auction.minimumNextBid)}
          aria-invalid={error ? true : undefined}
          aria-describedby="bid-help"
          className="tabular h-12 border-transparent text-lg"
          {...form.register('maxAmount')}
        />
        <Button type="submit" size="lg" loading={placeBid.isPending} className="px-7">
          Bid
        </Button>
      </div>
      {error && (
        <p role="alert" className="text-sm text-flare-300">
          {error}
        </p>
      )}
      {!isLeading && (
        <div className="flex flex-wrap gap-2" aria-label="Quick bids">
          {quickBids(auction.minimumNextBid, auction.increment).map((amount) => (
            <button
              key={amount}
              type="button"
              className="tabular cursor-pointer rounded-full bg-white/10 px-3 py-1.5 text-sm font-medium text-white hover:bg-white/20"
              onClick={() => form.setValue('maxAmount', String(amount), { shouldValidate: true })}
            >
              {formatMoney(amount)}
            </button>
          ))}
        </div>
      )}
      <p id="bid-help" className="text-sm text-white/60">
        Enter the most you would pay. Gavel bids for you one step at a time, only as high as it needs to.
      </p>
    </form>
  )
}
