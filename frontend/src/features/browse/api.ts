import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { api } from '@/lib/api/client'
import type { AuctionCategory, AuctionSummary, MarketStats, Paged } from '@/lib/api/types'
import { queryKeys } from '@/lib/query/keys'

export type ListStatus = 'Live' | 'Upcoming' | 'Ended'

export type AuctionSort = 'EndingSoon' | 'Newest' | 'PriceLow' | 'PriceHigh' | 'MostBids'

export type AuctionQuery = {
  status?: ListStatus
  category?: AuctionCategory
  search?: string
  sort?: AuctionSort
  page?: number
  pageSize?: number
}

export const useAuctions = (query: AuctionQuery) =>
  useQuery({
    queryKey: queryKeys.auctionList(query),
    queryFn: ({ signal }) => api<Paged<AuctionSummary>>('/api/auctions', { query, signal }),
    placeholderData: keepPreviousData,
  })

export const useMarketStats = () =>
  useQuery({
    queryKey: queryKeys.stats,
    queryFn: ({ signal }) => api<MarketStats>('/api/auctions/stats', { signal }),
    refetchInterval: 30_000,
  })
