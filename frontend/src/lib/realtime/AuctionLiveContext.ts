import { createContext, useContext, useEffect } from 'react'

export type AuctionLive = {
  subscribe: (auctionId: string) => () => void
  subscribeLobby: () => () => void
}

export const AuctionLiveContext = createContext<AuctionLive | null>(null)

function useAuctionLive() {
  const context = useContext(AuctionLiveContext)
  if (!context) throw new Error('useAuctionLive must be used inside <AuctionLiveProvider>')
  return context
}

export function useLiveAuction(auctionId: string | undefined) {
  const live = useAuctionLive()
  useEffect(() => (auctionId ? live.subscribe(auctionId) : undefined), [live, auctionId])
}

export function useLiveLobby() {
  const live = useAuctionLive()
  useEffect(() => live.subscribeLobby(), [live])
}
