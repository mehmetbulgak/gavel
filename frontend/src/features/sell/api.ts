import { useMutation, useQueryClient } from '@tanstack/react-query'
import { api } from '@/lib/api/client'
import type { AuctionCategory, AuctionSummary } from '@/lib/api/types'
import { queryKeys } from '@/lib/query/keys'

export type NewAuction = {
  title: string
  description: string | null
  category: AuctionCategory
  startingPrice: number
  reservePrice: number | null
  startsAt: string | null
  durationMinutes: number
}

export function useCreateAuction() {
  const client = useQueryClient()

  return useMutation({
    mutationFn: (auction: NewAuction) => api<AuctionSummary>('/api/auctions', { method: 'POST', body: auction }),
    onSuccess: () => {
      void client.invalidateQueries({ queryKey: queryKeys.auctions })
      void client.invalidateQueries({ queryKey: queryKeys.activity })
    },
  })
}
