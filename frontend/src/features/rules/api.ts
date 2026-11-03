import { useQuery } from '@tanstack/react-query'
import { api } from '@/lib/api/client'
import type { AuctionCategory } from '@/lib/api/types'

export type AuctionRules = {
  categories: AuctionCategory[]
  durationsInMinutes: number[]
  titleMinLength: number
  titleMaxLength: number
  descriptionMaxLength: number
  minStartingPrice: number
  maxStartingPrice: number
  maxBidAmount: number
  maxScheduleAheadMinutes: number
  snipeProtectionSeconds: number
}

export const useAuctionRules = () =>
  useQuery({
    queryKey: ['auction-rules'],
    queryFn: ({ signal }) => api<AuctionRules>('/api/auctions/rules', { signal }),
    staleTime: Infinity,
    gcTime: Infinity,
  })
