import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { api } from '@/lib/api/client'
import type { AuctionSummary, MyBid, Paged } from '@/lib/api/types'
import { queryKeys } from '@/lib/query/keys'

export type ActivityTab = 'bidding' | 'won' | 'lost' | 'selling' | 'watching'

const PAGE_SIZE = 20

const bidStates: Partial<Record<ActivityTab, string>> = { bidding: 'Active', won: 'Won', lost: 'Lost' }

export const useMyBids = (tab: ActivityTab, page: number) =>
  useQuery({
    queryKey: queryKeys.activityTab(tab, page),
    queryFn: ({ signal }) => api<Paged<MyBid>>('/api/me/bids', { query: { state: bidStates[tab], page, pageSize: PAGE_SIZE }, signal }),
    enabled: tab in bidStates,
    placeholderData: keepPreviousData,
  })

export const useMyAuctions = (tab: ActivityTab, page: number) =>
  useQuery({
    queryKey: queryKeys.activityTab(tab, page),
    queryFn: ({ signal }) =>
      api<Paged<AuctionSummary>>(tab === 'watching' ? '/api/me/watchlist' : '/api/me/auctions', {
        query: { page, pageSize: PAGE_SIZE },
        signal,
      }),
    enabled: tab === 'selling' || tab === 'watching',
    placeholderData: keepPreviousData,
  })
