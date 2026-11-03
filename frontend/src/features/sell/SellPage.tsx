import { zodResolver } from '@hookform/resolvers/zod'
import { AlertTriangle } from 'lucide-react'
import { useMemo, useState } from 'react'
import { useForm, useWatch } from 'react-hook-form'
import { useNavigate } from 'react-router'
import { toast } from 'sonner'
import { Button } from '@/components/ui/Button'
import { EmptyState } from '@/components/ui/EmptyState'
import { Field } from '@/components/ui/Field'
import { Input, Select, Textarea } from '@/components/ui/FormControls'
import { FormError } from '@/components/ui/FormError'
import { SegmentedControl } from '@/components/ui/SegmentedControl'
import { Skeleton } from '@/components/ui/Skeleton'
import { categoryLabels } from '@/features/browse/categoryLabels'
import { type AuctionRules, useAuctionRules } from '@/features/rules/api'
import { formatDuration, formatShortDuration } from '@/features/rules/formatDuration'
import { ApiError, describeError } from '@/lib/api/errors'
import { AUCTION_CATEGORIES } from '@/lib/api/types'
import { toLocalInputValue } from '@/lib/dates'
import { formatNumber } from '@/lib/format'
import { useCreateAuction } from './api'
import { ListingPreview } from './ListingPreview'
import { createSellSchema, type SellInput, type SellValues } from './sellSchema'

const DEFAULT_DURATION = 1440
const FIELDS = ['title', 'category', 'description', 'startingPrice', 'reservePrice', 'durationMinutes', 'startsAt'] as const

type FieldName = (typeof FIELDS)[number]

const toFieldName = (key: string) => FIELDS.find((field) => field.toLowerCase() === key.toLowerCase())

export default function SellPage() {
  const rules = useAuctionRules()

  return (
    <div className="grid grid-cols-1 gap-8">
      <title>Sell an item | Gavel</title>
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Sell an item</h1>
        <p className="mt-1 max-w-2xl text-muted">
          Set a starting price and a closing time. Bidders compete with maximum bids, and the price only rises as far as it needs to.
        </p>
      </div>
      {rules.isPending ? (
        <Skeleton className="h-[36rem] rounded-3xl" />
      ) : rules.isError ? (
        <EmptyState icon={<AlertTriangle />} title="The listing form could not be loaded" description="Refresh the page to try again." />
      ) : (
        <SellForm rules={rules.data} />
      )}
    </div>
  )
}

function SellForm({ rules }: { rules: AuctionRules }) {
  const navigate = useNavigate()
  const create = useCreateAuction()
  const [error, setError] = useState<string | null>(null)
  const schema = useMemo(() => createSellSchema(rules), [rules])
  const durations = useMemo(
    () => rules.durationsInMinutes.map((minutes) => ({ value: String(minutes), label: formatShortDuration(minutes) })),
    [rules.durationsInMinutes],
  )
  const defaultDuration = rules.durationsInMinutes.includes(DEFAULT_DURATION)
    ? DEFAULT_DURATION
    : (rules.durationsInMinutes[0] ?? DEFAULT_DURATION)

  const form = useForm<SellInput, unknown, SellValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      title: '',
      category: '',
      description: '',
      startingPrice: '',
      reservePrice: '',
      durationMinutes: String(defaultDuration),
      startMode: 'now',
      startsAt: '',
    },
  })
  const { errors, isSubmitting } = form.formState
  const [openedAt] = useState(Date.now)
  const [title, category, description, startingPrice, reservePrice, durationMinutes, startMode] = useWatch({
    control: form.control,
    name: ['title', 'category', 'description', 'startingPrice', 'reservePrice', 'durationMinutes', 'startMode'],
  })

  const submit = async (values: SellValues) => {
    setError(null)
    try {
      const auction = await create.mutateAsync({
        title: values.title,
        description: values.description || null,
        category: values.category,
        startingPrice: values.startingPrice,
        reservePrice: values.reservePrice ?? null,
        startsAt: values.startMode === 'later' ? new Date(values.startsAt).toISOString() : null,
        durationMinutes: values.durationMinutes,
      })
      toast.success(auction.status === 'Scheduled' ? 'Auction scheduled' : 'Auction published')
      void navigate(`/auctions/${auction.id}`)
    } catch (e) {
      if (e instanceof ApiError && Object.keys(e.fieldErrors).length > 0) {
        let mapped = false
        for (const [key, messages] of Object.entries(e.fieldErrors)) {
          const field: FieldName | undefined = toFieldName(key)
          if (field && messages[0]) {
            form.setError(field, { message: messages[0] })
            mapped = true
          }
        }
        if (mapped) return
      }
      setError(describeError(e))
    }
  }

  return (
    <div className="grid grid-cols-1 gap-10 lg:grid-cols-[minmax(0,1fr)_320px] lg:items-start">
      <form className="grid gap-8" onSubmit={form.handleSubmit(submit)} noValidate>
        <FormError message={error} />

        <fieldset className="grid gap-5 rounded-3xl bg-white p-6 ring-1 ring-line sm:p-8">
          <legend className="float-left mb-1 font-display text-lg font-semibold">What you are selling</legend>
          <Field label="Title" error={errors.title?.message} hint="Brand, model and condition help buyers find it.">
            {(props) => <Input maxLength={rules.titleMaxLength} autoComplete="off" {...props} {...form.register('title')} />}
          </Field>
          <Field label="Category" error={errors.category?.message}>
            {(props) => (
              <Select {...props} {...form.register('category')}>
                <option value="" disabled>
                  Choose a category
                </option>
                {AUCTION_CATEGORIES.map((value) => (
                  <option key={value} value={value}>
                    {categoryLabels[value]}
                  </option>
                ))}
              </Select>
            )}
          </Field>
          <Field
            label="Description (optional)"
            error={errors.description?.message}
            hint={`${formatNumber(description.length)} of ${formatNumber(rules.descriptionMaxLength)} characters`}
          >
            {(props) => <Textarea rows={5} maxLength={rules.descriptionMaxLength} {...props} {...form.register('description')} />}
          </Field>
        </fieldset>

        <fieldset className="grid gap-5 rounded-3xl bg-white p-6 ring-1 ring-line sm:grid-cols-2 sm:p-8">
          <legend className="float-left mb-1 font-display text-lg font-semibold sm:col-span-2">Price</legend>
          <Field label="Starting price (₺)" error={errors.startingPrice?.message}>
            {(props) => (
              <Input
                type="number"
                inputMode="decimal"
                step="0.01"
                min={rules.minStartingPrice}
                {...props}
                {...form.register('startingPrice')}
              />
            )}
          </Field>
          <Field
            label="Reserve price (₺, optional)"
            error={errors.reservePrice?.message}
            hint="Hidden from bidders. Below it, the item does not sell."
          >
            {(props) => (
              <Input
                type="number"
                inputMode="decimal"
                step="0.01"
                min={rules.minStartingPrice}
                {...props}
                {...form.register('reservePrice')}
              />
            )}
          </Field>
        </fieldset>

        <fieldset className="grid gap-6 rounded-3xl bg-white p-6 ring-1 ring-line sm:p-8">
          <legend className="float-left mb-1 font-display text-lg font-semibold">Timing</legend>
          <div className="grid gap-2">
            <p className="text-sm font-medium">Start</p>
            <SegmentedControl
              label="Start"
              value={startMode}
              options={[
                { value: 'now', label: 'Right away' },
                { value: 'later', label: 'At a set time' },
              ]}
              onChange={(value) => form.setValue('startMode', value, { shouldValidate: form.formState.isSubmitted })}
              className="w-fit"
            />
          </div>
          {startMode === 'later' && (
            <Field
              label="Opens at"
              error={errors.startsAt?.message}
              hint={`Your local time. Up to ${formatDuration(rules.maxScheduleAheadMinutes)} ahead.`}
              className="max-w-xs"
            >
              {(props) => (
                <Input
                  type="datetime-local"
                  min={toLocalInputValue(openedAt)}
                  max={toLocalInputValue(openedAt + rules.maxScheduleAheadMinutes * 60_000)}
                  {...props}
                  {...form.register('startsAt')}
                />
              )}
            </Field>
          )}
          <div className="grid gap-2">
            <p className="text-sm font-medium">Runs for</p>
            <SegmentedControl
              label="Runs for"
              value={durationMinutes}
              options={durations}
              onChange={(value) => form.setValue('durationMinutes', value, { shouldValidate: form.formState.isSubmitted })}
              className="w-fit"
            />
            {errors.durationMinutes?.message && <p className="text-sm text-flare-700">{errors.durationMinutes.message}</p>}
          </div>
        </fieldset>

        <div className="flex flex-wrap items-center gap-4">
          <Button type="submit" size="lg" loading={isSubmitting}>
            {startMode === 'later' ? 'Schedule auction' : 'Publish auction'}
          </Button>
          <p className="text-sm text-muted">You can withdraw it until the first bid arrives.</p>
        </div>
      </form>

      <aside className="grid gap-3 lg:sticky lg:top-24">
        <p className="text-sm font-medium text-ink-2">How bidders will see it</p>
        <ListingPreview
          title={title}
          category={category}
          startingPrice={startingPrice}
          durationMinutes={durationMinutes}
          hasReserve={reservePrice.trim() !== ''}
        />
      </aside>
    </div>
  )
}
