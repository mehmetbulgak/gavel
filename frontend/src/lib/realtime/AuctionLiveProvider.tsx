import { HubConnectionState, type HubConnection } from '@microsoft/signalr'
import { useQueryClient } from '@tanstack/react-query'
import { useCallback, useEffect, useMemo, useRef, type ReactNode } from 'react'
import type { AuctionUpdate } from '../api/types'
import { queryKeys } from '../query/keys'
import { AuctionLiveContext, type AuctionLive } from './AuctionLiveContext'
import { applyAuctionUpdate } from './applyAuctionUpdate'
import { createHubConnection, startWithRetry } from './createHubConnection'

const HUB_URL = '/hubs/auctions'

const ignore = () => undefined

export function AuctionLiveProvider({ children }: { children: ReactNode }) {
  const client = useQueryClient()
  const connectionRef = useRef<HubConnection | null>(null)
  const auctions = useRef(new Map<string, number>())
  const lobby = useRef(0)

  const invoke = useCallback((method: string, ...args: unknown[]) => {
    const connection = connectionRef.current
    if (connection?.state === HubConnectionState.Connected) {
      connection.invoke(method, ...args).catch(ignore)
    }
  }, [])

  const resubscribe = useCallback(() => {
    auctions.current.forEach((_, auctionId) => invoke('SubscribeToAuction', auctionId))
    if (lobby.current > 0) invoke('SubscribeToLobby')
  }, [invoke])

  useEffect(() => {
    let stopped = false
    const connection = createHubConnection(HUB_URL)
    connectionRef.current = connection
    connection.on('AuctionChanged', (update: AuctionUpdate) => applyAuctionUpdate(client, update))
    connection.on('AuctionPublished', () => void client.invalidateQueries({ queryKey: queryKeys.auctionLists }))
    const resync = () => {
      resubscribe()
      void client.invalidateQueries({ queryKey: queryKeys.auctions })
    }
    connection.onreconnected(resync)
    connection.onclose(() => {
      if (!stopped) void startWithRetry(connection, () => stopped, resync)
    })
    void startWithRetry(connection, () => stopped, resubscribe)

    return () => {
      stopped = true
      connectionRef.current = null
      void connection.stop()
    }
  }, [client, resubscribe])

  const value = useMemo<AuctionLive>(
    () => ({
      subscribe: (auctionId) => {
        const count = (auctions.current.get(auctionId) ?? 0) + 1
        auctions.current.set(auctionId, count)
        if (count === 1) invoke('SubscribeToAuction', auctionId)

        return () => {
          const remaining = (auctions.current.get(auctionId) ?? 1) - 1
          if (remaining > 0) {
            auctions.current.set(auctionId, remaining)
            return
          }

          auctions.current.delete(auctionId)
          invoke('UnsubscribeFromAuction', auctionId)
        }
      },
      subscribeLobby: () => {
        lobby.current += 1
        if (lobby.current === 1) invoke('SubscribeToLobby')

        return () => {
          lobby.current -= 1
          if (lobby.current === 0) invoke('UnsubscribeFromLobby')
        }
      },
    }),
    [invoke],
  )

  return <AuctionLiveContext.Provider value={value}>{children}</AuctionLiveContext.Provider>
}
