import React from 'react'
import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth'
import { Skeleton } from '@/shared/components/ui/Skeleton'

/** Guard that requires the user to be authenticated */
export const ProtectedRoute: React.FC = () => {
  const { isAuthenticated, isLoading, token } = useAuth()

  // If we have a token and are loading the /me profile
  if (token && isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center p-6 bg-background">
        <div className="w-full max-w-sm space-y-4">
          <Skeleton className="h-8 w-48 mx-auto" />
          <Skeleton className="h-4 w-64 mx-auto" />
          <Skeleton className="h-32 w-full rounded-xl" />
        </div>
      </div>
    )
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />
  }

  return <Outlet />
}

/** Guard that prevents authenticated users from visiting login/register */
export const PublicOnlyRoute: React.FC = () => {
  const { isAuthenticated } = useAuth()

  if (isAuthenticated) {
    return <Navigate to="/" replace />
  }

  return <Outlet />
}
