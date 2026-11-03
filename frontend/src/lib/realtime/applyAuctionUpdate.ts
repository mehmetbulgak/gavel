import type { QueryClient } from '@tanstack/react-query'
import type { AuctionDetails, AuctionSummary, AuctionUpdate, Bid, Paged } from '../api/types'
import { queryKeys } from '../query/keys'

const RECENT_BIDS = 20

const mergeBids = (existing: Bid[], incoming: Bid[], myAlias: string | null | undefined, limit?: number) => {
  const known = new Set(existing.map((bid) => bid.id))
  const fresh = incoming.filter((bid) => !known.has(bid.id)).map((bid) => ({ ...bid, isMine: Boolean(myAlias) && bid.alias === myAlias }))
  const merged = [...fresh, ...existing].sort((a, b) => b.sequence - a.sequence)
  return limit ? merged.slice(0, limit) : merged
}

export function applyAuctionUpdate(client: QueryClient, update: AuctionUpdate) {
  const detailKey = queryKeys.auction(update.auctionId)
  const myAlias = client.getQueryData<AuctionDetails>(detailKey)?.viewer?.alias

  client.setQueryData<AuctionDetails>(detailKey, (old) =>
    old
      ? {
          ...old,
          status: update.status,
          currentPrice: update.currentPrice,
          minimumNextBid: update.minimumNextBid,
          bidCount: update.bidCount,
          bidderCount: update.bidderCount,
          endsAt: update.endsAt,
          leaderAlias: update.leaderAlias,
          reserveMet: update.reserveMet,
          outcome: update.outcome,
          recentBids: mergeBids(old.recentBids, update.newBids, myAlias, RECENT_BIDS),
          viewer: old.viewer && { ...old.viewer, isLeading: Boolean(old.viewer.alias) && old.viewer.alias === update.leaderAlias },
        }
      : old,
  )

  client.setQueryData<Paged<Bid>>(queryKeys.bids(update.auctionId), (old) =>
    old && update.newBids.length > 0 ? { ...old, items: mergeBids(old.items, update.newBids, myAlias), totalCount: update.bidCount } : old,
  )

  client.setQueriesData<Paged<AuctionSummary>>({ queryKey: queryKeys.auctionLists }, (page) =>
    page
      ? {
          ...page,
          items: page.items.map((item) =>
            item.id === update.auctionId
              ? {
                  ...item,
                  status: update.status,
                  currentPrice: update.currentPrice,
                  bidCount: update.bidCount,
                  endsAt: update.endsAt,
                  reserveMet: update.reserveMet,
                  outcome: update.outcome,
                }
              : item,
          ),
        }
      : page,
  )

  if (update.status !== 'Live') {
    void client.invalidateQueries({ queryKey: detailKey })
    void client.invalidateQueries({ queryKey: queryKeys.auctionLists })
    void client.invalidateQueries({ queryKey: queryKeys.activity })
  }
}
