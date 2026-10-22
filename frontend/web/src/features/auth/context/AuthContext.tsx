import React, { createContext, useState, useCallback } from 'react'
import { useQuery } from '@tanstack/react-query'
import { authApi } from '../api/authApi'
import { queryClient } from '@/app/queryClient'
import { UserDto } from '../types'

export interface AuthContextType {
  token: string | null
  user: UserDto | null
  isAuthenticated: boolean
  isLoading: boolean
  setAuthToken: (token: string) => void
  logout: () => void
}

// oxlint-disable-next-line react/only-export-components
export const AuthContext = createContext<AuthContextType | undefined>(undefined)

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {

  const [token, setToken] = useState<string | null>(null)

  const {
    data: user,
    isLoading: isUserLoading,
    error,
  } = useQuery({
    queryKey: ['auth', 'me', token],
    queryFn: () => (token ? authApi.getMe(token) : Promise.resolve(null)),
    enabled: !!token,
    staleTime: 1000 * 60 * 5,
  })

  const logout = useCallback(() => {
    if (token) {
      authApi.logout(token).catch(() => {})
    }
    setToken(null)
    queryClient.clear()
  }, [token])

  // If token is invalid (e.g. 401), automatically clear token
  if (error && token) {
    setToken(null)
    queryClient.clear()
  }

  const setAuthToken = useCallback((newToken: string) => {
    setToken(newToken)
  }, [])

  const value: AuthContextType = {
    token,
    user: user ?? null,
    isAuthenticated: !!token && !!user,
    isLoading: !!token && isUserLoading,
    setAuthToken,
    logout,
  }

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
