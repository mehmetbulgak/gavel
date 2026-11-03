import * as Popover from '@radix-ui/react-popover'
import { useState } from 'react'
import { toast } from 'sonner'
import { Button } from '@/components/ui/Button'
import { describeError } from '@/lib/api/errors'
import { useCancelAuction } from './api'

export function WithdrawButton({ auctionId }: { auctionId: string }) {
  const [open, setOpen] = useState(false)
  const cancel = useCancelAuction(auctionId)

  const withdraw = () =>
    cancel.mutate(undefined, {
      onSuccess: () => {
        setOpen(false)
        toast.success('Auction withdrawn')
      },
      onError: (error) => toast.error(describeError(error)),
    })

  return (
    <Popover.Root open={open} onOpenChange={setOpen}>
      <Popover.Trigger asChild>
        <Button variant="danger" className="flex-1">
          Withdraw
        </Button>
      </Popover.Trigger>
      <Popover.Portal>
        <Popover.Content
          align="end"
          sideOffset={8}
          className="z-50 w-72 rounded-2xl bg-white p-4 shadow-[0_18px_50px_-12px_rgb(22_26_53/0.35)] ring-1 ring-line focus:outline-none"
        >
          <p className="font-medium">Withdraw this auction?</p>
          <p className="mt-1 text-sm text-muted">Nobody has bid yet. Once withdrawn it cannot be reopened.</p>
          <div className="mt-4 flex justify-end gap-2">
            <Popover.Close asChild>
              <Button variant="ghost" size="sm">
                Keep it
              </Button>
            </Popover.Close>
            <Button variant="danger" size="sm" loading={cancel.isPending} onClick={withdraw}>
              Withdraw
            </Button>
          </div>
        </Popover.Content>
      </Popover.Portal>
    </Popover.Root>
  )
}
