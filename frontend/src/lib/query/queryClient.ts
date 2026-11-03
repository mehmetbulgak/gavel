import { QueryClient } from '@tanstack/react-query'
import { ApiError } from '../api/errors'

const MAX_RETRIES = 2

export function createQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: {
        staleTime: 15_000,
        retry: (failureCount, error) => !(error instanceof ApiError && error.status < 500) && failureCount < MAX_RETRIES,
      },
    },
  })
}
