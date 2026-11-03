import { Bookmark, BookmarkCheck } from 'lucide-react'
import { toast } from 'sonner'
import { Button } from '@/components/ui/Button'
import { describeError } from '@/lib/api/errors'
import { useWatch } from './api'

export function WatchButton({ auctionId, watching }: { auctionId: string; watching: boolean }) {
  const watch = useWatch(auctionId)

  const toggle = () =>
    watch.mutate(!watching, {
      onSuccess: () =>
        toast.success(watching ? 'Removed from your watchlist' : 'Added to your watchlist', {
          description: watching ? undefined : 'We will let you know before it closes.',
        }),
      onError: (error) => toast.error(describeError(error)),
    })

  return (
    <Button variant="secondary" aria-pressed={watching} loading={watch.isPending} onClick={toggle} className="flex-1">
      {watching ? <BookmarkCheck className="text-cobalt-600" /> : <Bookmark />}
      {watching ? 'Watching' : 'Watch'}
    </Button>
  )
}
