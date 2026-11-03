import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/lib/api/client'
import type { AuctionDetails, Bid, BidResult, Paged } from '@/lib/api/types'
import { queryKeys } from '@/lib/query/keys'
import { syncServerTime } from '@/lib/time/serverClock'

const HISTORY_SIZE = 100

const auctionUrl = (id: string) => `/api/auctions/${encodeURIComponent(id)}`

export const useAuction = (id: string) =>
  useQuery({
    queryKey: queryKeys.auction(id),
    queryFn: async ({ signal }) => {
      const auction = await api<AuctionDetails>(auctionUrl(id), { signal })
      syncServerTime(auction.serverTime)
      return auction
    },
  })

export const useBidHistory = (id: string) =>
  useQuery({
    queryKey: queryKeys.bids(id),
    queryFn: ({ signal }) => api<Paged<Bid>>(`${auctionUrl(id)}/bids`, { query: { pageSize: HISTORY_SIZE }, signal }),
  })

export function usePlaceBid(id: string) {
  const client = useQueryClient()

  return useMutation({
    mutationFn: (maxAmount: number) => api<BidResult>(`${auctionUrl(id)}/bids`, { method: 'POST', body: { maxAmount } }),
    onSuccess: (result) => {
      client.setQueryData<AuctionDetails>(queryKeys.auction(id), (old) =>
        old
          ? {
              ...old,
              currentPrice: result.currentPrice,
              minimumNextBid: result.minimumNextBid,
              bidCount: result.bidCount,
              endsAt: result.endsAt,
              viewer: old.viewer && { ...old.viewer, isLeading: result.isLeading, alias: result.alias, maxBid: result.yourMaxBid },
            }
          : old,
      )
      void client.invalidateQueries({ queryKey: queryKeys.auction(id) })
      void client.invalidateQueries({ queryKey: queryKeys.bids(id) })
      void client.invalidateQueries({ queryKey: queryKeys.activity })
    },
  })
}

export function useWatch(id: string) {
  const client = useQueryClient()

  return useMutation({
    mutationFn: (watch: boolean) => api<void>(`${auctionUrl(id)}/watch`, { method: watch ? 'PUT' : 'DELETE' }),
    onSuccess: (_, watch) => {
      client.setQueryData<AuctionDetails>(queryKeys.auction(id), (old) =>
        old?.viewer ? { ...old, viewer: { ...old.viewer, isWatching: watch } } : old,
      )
      void client.invalidateQueries({ queryKey: queryKeys.activity })
    },
  })
}

export function useCancelAuction(id: string) {
  const client = useQueryClient()

  return useMutation({
    mutationFn: () => api<void>(`${auctionUrl(id)}/cancel`, { method: 'POST' }),
    onSuccess: () => {
      void client.invalidateQueries({ queryKey: queryKeys.auctions })
      void client.invalidateQueries({ queryKey: queryKeys.activity })
    },
  })
}
