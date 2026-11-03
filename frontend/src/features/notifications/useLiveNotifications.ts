import { useQueryClient } from '@tanstack/react-query'
import { useEffect } from 'react'
import { toast } from 'sonner'
import { getFreshAccessToken } from '@/lib/api/session'
import type { AppNotification } from '@/lib/api/types'
import { queryKeys } from '@/lib/query/keys'
import { createHubConnection, startWithRetry } from '@/lib/realtime/createHubConnection'

const HUB_URL = '/hubs/notifications'

const accessToken = async () => (await getFreshAccessToken()) ?? ''

export function useLiveNotifications(userId: string | null) {
  const client = useQueryClient()

  useEffect(() => {
    if (!userId) return undefined

    let stopped = false
    const connection = createHubConnection(HUB_URL, accessToken)
    connection.on('NotificationReceived', (notification: AppNotification) => {
      toast(notification.title, { description: notification.body })
      void client.invalidateQueries({ queryKey: queryKeys.notifications })
      void client.invalidateQueries({ queryKey: queryKeys.activity })
    })
    const resync = () => void client.invalidateQueries({ queryKey: queryKeys.notifications })
    connection.onreconnected(resync)
    connection.onclose(() => {
      if (!stopped) void startWithRetry(connection, () => stopped, resync)
    })
    void startWithRetry(connection, () => stopped, resync)

    return () => {
      stopped = true
      void connection.stop()
    }
  }, [client, userId])
}
