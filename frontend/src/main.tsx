import { QueryClientProvider } from '@tanstack/react-query'
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { Toaster } from 'sonner'
import App from './App'
import { AuthProvider } from './lib/auth/AuthProvider'
import { createQueryClient } from './lib/query/queryClient'
import { AuctionLiveProvider } from './lib/realtime/AuctionLiveProvider'
import './index.css'

const queryClient = createQueryClient()
const root = document.getElementById('root')

if (root) {
  createRoot(root).render(
    <StrictMode>
      <QueryClientProvider client={queryClient}>
        <AuthProvider>
          <AuctionLiveProvider>
            <App />
            <Toaster position="bottom-right" richColors closeButton />
          </AuctionLiveProvider>
        </AuthProvider>
      </QueryClientProvider>
    </StrictMode>,
  )
}
