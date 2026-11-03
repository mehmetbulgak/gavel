export const queryKeys = {
  auctions: ['auctions'] as const,
  auctionLists: ['auctions', 'list'] as const,
  auctionList: (params: object) => ['auctions', 'list', params] as const,
  auction: (id: string) => ['auctions', 'detail', id] as const,
  bids: (id: string) => ['auctions', 'bids', id] as const,
  stats: ['auctions', 'stats'] as const,
  activity: ['activity'] as const,
  activityTab: (tab: string, page: number) => ['activity', tab, page] as const,
  notifications: ['notifications'] as const,
  notificationList: (params: object) => ['notifications', 'list', params] as const,
}
