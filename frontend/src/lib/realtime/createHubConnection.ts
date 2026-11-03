import { HubConnectionBuilder, LogLevel, type HubConnection } from '@microsoft/signalr'

const RECONNECT_DELAYS_MS = [0, 2000, 5000, 10_000, 30_000]
const RETRY_START_MS = 3000

export function createHubConnection(url: string, accessTokenFactory?: () => Promise<string>): HubConnection {
  return new HubConnectionBuilder()
    .withUrl(url, { accessTokenFactory, withCredentials: false })
    .withAutomaticReconnect(RECONNECT_DELAYS_MS)
    .configureLogging(LogLevel.None)
    .build()
}

export async function startWithRetry(connection: HubConnection, isStopped: () => boolean, onStarted: () => Promise<void> | void) {
  while (!isStopped()) {
    try {
      await connection.start()
      await onStarted()
      return
    } catch {
      await new Promise((resolve) => setTimeout(resolve, RETRY_START_MS))
    }
  }
}
