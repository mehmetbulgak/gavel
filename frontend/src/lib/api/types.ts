export type User = {
  id: string
  email: string
  displayName: string
}

export type AuthResponse = {
  accessToken: string
  expiresAt: string
  user: User
}

export type Paged<T> = {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export const AUCTION_CATEGORIES = [
  'Electronics',
  'Watches',
  'Fashion',
  'Home',
  'Collectibles',
  'Art',
  'Music',
  'Sports',
  'Books',
  'Toys',
] as const

export type AuctionCategory = (typeof AUCTION_CATEGORIES)[number]

export type AuctionStatus = 'Scheduled' | 'Live' | 'Ended' | 'Cancelled'

export type AuctionOutcome = 'Sold' | 'NoBids' | 'ReserveNotMet'

export type AuctionSummary = {
  id: string
  title: string
  category: AuctionCategory
  status: AuctionStatus
  currentPrice: number
  bidCount: number
  startsAt: string
  endsAt: string
  sellerName: string
  hasReserve: boolean
  reserveMet: boolean
  outcome: AuctionOutcome | null
}

export type Bid = {
  id: string
  alias: string
  amount: number
  isAutomatic: boolean
  sequence: number
  placedAt: string
  isMine: boolean
}

export type Viewer = {
  isSeller: boolean
  isWatching: boolean
  isLeading: boolean
  alias: string | null
  maxBid: number | null
  isWinner: boolean
}

export type AuctionDetails = {
  id: string
  title: string
  description: string
  category: AuctionCategory
  status: AuctionStatus
  sellerName: string
  startingPrice: number
  currentPrice: number
  minimumNextBid: number
  increment: number
  bidCount: number
  bidderCount: number
  startsAt: string
  endsAt: string
  extensionCount: number
  hasReserve: boolean
  reserveMet: boolean
  reservePrice: number | null
  leaderAlias: string | null
  outcome: AuctionOutcome | null
  finalPrice: number | null
  serverTime: string
  viewer: Viewer | null
  recentBids: Bid[]
}

export type AuctionUpdate = {
  auctionId: string
  status: AuctionStatus
  currentPrice: number
  minimumNextBid: number
  bidCount: number
  bidderCount: number
  endsAt: string
  leaderAlias: string | null
  reserveMet: boolean
  outcome: AuctionOutcome | null
  newBids: Bid[]
}

export type BidResult = {
  isLeading: boolean
  alias: string
  yourMaxBid: number
  currentPrice: number
  minimumNextBid: number
  bidCount: number
  endsAt: string
  extended: boolean
}

export type MarketStats = {
  liveAuctions: number
  endingWithinHour: number
  bidsLastHour: number
  soldLastDay: number
}

export type MyBid = {
  auction: AuctionSummary
  alias: string
  maxBid: number
  isLeading: boolean
  isWinner: boolean
}

export type NotificationType = 'Welcome' | 'Outbid' | 'AuctionWon' | 'AuctionSold' | 'AuctionUnsold' | 'AuctionLost' | 'AuctionEndingSoon'

export type AppNotification = {
  id: string
  type: NotificationType
  title: string
  body: string
  auctionId: string | null
  createdAt: string
  isRead: boolean
}

export type NotificationPage = {
  items: AppNotification[]
  page: number
  pageSize: number
  totalCount: number
  unreadCount: number
}
