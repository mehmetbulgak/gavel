import { CircleAlert, CircleCheck, Store, Trophy } from 'lucide-react'
import type { ReactNode } from 'react'
import { cn } from '@/lib/cn'
import { formatMoney } from '@/lib/format'
import type { ViewerStanding } from './auctionState'

type Banner = { icon: ReactNode; tone: string; text: string }

function bannerFor(standing: NonNullable<ViewerStanding>): Banner {
  switch (standing.kind) {
    case 'seller':
      return { icon: <Store />, tone: 'bg-white/10 text-white', text: 'This is your listing, so you cannot bid on it.' }
    case 'leading':
      return {
        icon: <CircleCheck />,
        tone: 'bg-mint-500/20 text-mint-300',
        text: `You are winning. Your maximum of ${formatMoney(standing.maxBid)} stays private.`,
      }
    case 'outbid':
      return { icon: <CircleAlert />, tone: 'bg-flare-500/20 text-flare-300', text: 'You have been outbid. Bid again to take the lead.' }
    case 'won':
      return { icon: <Trophy />, tone: 'bg-mint-500/20 text-mint-300', text: 'You won this auction.' }
    case 'lost':
      return { icon: <CircleAlert />, tone: 'bg-white/10 text-white/80', text: 'Another bidder won this one.' }
  }
}

export function ViewerBanner({ standing }: { standing: ViewerStanding }) {
  if (!standing) return null
  const { icon, tone, text } = bannerFor(standing)

  return (
    <p
      role="status"
      className={cn(
        'flex items-start gap-2.5 rounded-xl px-3.5 py-3 text-sm font-medium [&_svg]:mt-px [&_svg]:size-4 [&_svg]:shrink-0',
        tone,
      )}
    >
      {icon}
      {text}
    </p>
  )
}
