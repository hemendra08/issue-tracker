import { QueryClient } from '@tanstack/react-query'

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: (failureCount, error) => {
        // Don't retry on client errors like 401 Unauthorized or 404
        if (error && 'status' in error && (error.status === 401 || error.status === 403 || error.status === 404)) {
          return false
        }
        return failureCount < 1
      },
      staleTime: 1000 * 60 * 5, // 5 minutes
      refetchOnWindowFocus: false,
    },
  },
})
