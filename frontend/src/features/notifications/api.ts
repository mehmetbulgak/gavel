import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/lib/api/client'
import type { NotificationPage } from '@/lib/api/types'
import { queryKeys } from '@/lib/query/keys'

export type NotificationQuery = { unreadOnly?: boolean; page?: number; pageSize?: number }

export const useNotifications = (query: NotificationQuery, enabled = true) =>
  useQuery({
    queryKey: queryKeys.notificationList(query),
    queryFn: ({ signal }) => api<NotificationPage>('/api/notifications', { query, signal }),
    placeholderData: keepPreviousData,
    enabled,
  })

export function useMarkRead() {
  const client = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => api<void>(`/api/notifications/${encodeURIComponent(id)}/read`, { method: 'POST' }),
    onSuccess: () => void client.invalidateQueries({ queryKey: queryKeys.notifications }),
  })
}

export function useMarkAllRead() {
  const client = useQueryClient()

  return useMutation({
    mutationFn: () => api<void>('/api/notifications/read-all', { method: 'POST' }),
    onSuccess: () => void client.invalidateQueries({ queryKey: queryKeys.notifications }),
  })
}
