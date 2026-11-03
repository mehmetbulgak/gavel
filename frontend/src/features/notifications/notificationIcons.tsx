import { BellRing, CircleSlash, Gavel, HandCoins, Hourglass, PartyPopper, TrendingUp, type LucideIcon } from 'lucide-react'
import type { NotificationType } from '@/lib/api/types'

export const notificationIcons: Record<NotificationType, { icon: LucideIcon; tone: string }> = {
  Welcome: { icon: PartyPopper, tone: 'bg-cobalt-50 text-cobalt-700' },
  Outbid: { icon: TrendingUp, tone: 'bg-flare-50 text-flare-700' },
  AuctionWon: { icon: Gavel, tone: 'bg-mint-50 text-mint-700' },
  AuctionSold: { icon: HandCoins, tone: 'bg-mint-50 text-mint-700' },
  AuctionUnsold: { icon: CircleSlash, tone: 'bg-ink/5 text-ink-2' },
  AuctionLost: { icon: BellRing, tone: 'bg-ink/5 text-ink-2' },
  AuctionEndingSoon: { icon: Hourglass, tone: 'bg-flare-50 text-flare-700' },
}
