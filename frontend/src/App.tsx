import { lazy, Suspense } from 'react'
import { createBrowserRouter, RouterProvider } from 'react-router'
import { AppShell } from '@/components/layout/AppShell'
import { NotFoundPage } from '@/components/NotFoundPage'
import { RouteError } from '@/components/RouteError'
import { GuestOnly, RequireAuth } from '@/components/RouteGuards'
import { Spinner } from '@/components/ui/Spinner'

const LoginPage = lazy(() => import('@/features/auth/LoginPage'))
const RegisterPage = lazy(() => import('@/features/auth/RegisterPage'))
const BrowsePage = lazy(() => import('@/features/browse/BrowsePage'))
const AuctionPage = lazy(() => import('@/features/auction/AuctionPage'))
const SellPage = lazy(() => import('@/features/sell/SellPage'))
const ActivityPage = lazy(() => import('@/features/activity/ActivityPage'))
const NotificationsPage = lazy(() => import('@/features/notifications/NotificationsPage'))

const router = createBrowserRouter([
  {
    path: '/login',
    errorElement: <RouteError />,
    element: (
      <GuestOnly>
        <Suspense fallback={<Spinner fullPage />}>
          <LoginPage />
        </Suspense>
      </GuestOnly>
    ),
  },
  {
    path: '/register',
    errorElement: <RouteError />,
    element: (
      <GuestOnly>
        <Suspense fallback={<Spinner fullPage />}>
          <RegisterPage />
        </Suspense>
      </GuestOnly>
    ),
  },
  {
    element: <AppShell />,
    errorElement: <RouteError />,
    children: [
      { index: true, element: <BrowsePage /> },
      { path: 'auctions/:id', element: <AuctionPage /> },
      {
        path: 'sell',
        element: (
          <RequireAuth>
            <SellPage />
          </RequireAuth>
        ),
      },
      {
        path: 'activity',
        element: (
          <RequireAuth>
            <ActivityPage />
          </RequireAuth>
        ),
      },
      {
        path: 'notifications',
        element: (
          <RequireAuth>
            <NotificationsPage />
          </RequireAuth>
        ),
      },
      { path: '*', element: <NotFoundPage /> },
    ],
  },
])

export default function App() {
  return <RouterProvider router={router} />
}
