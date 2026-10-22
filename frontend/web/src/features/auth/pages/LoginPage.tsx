import React, { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { Eye, EyeOff, LogIn } from 'lucide-react'
import { loginSchema, LoginFormData } from '../schemas/authSchemas'
import { authApi } from '../api/authApi'
import { useAuth } from '@/features/auth/hooks/useAuth'
import { Button } from '@/shared/components/ui/Button'
import { Input } from '@/shared/components/ui/Input'
import { Label } from '@/shared/components/ui/Label'
import { Alert } from '@/shared/components/ui/Alert'
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@/shared/components/ui/Card'

export const LoginPage: React.FC = () => {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const { setAuthToken } = useAuth()
  const [showPassword, setShowPassword] = useState(false)
  const [serverError, setServerError] = useState<string | null>(null)
  const isRegisteredSuccess = searchParams.get('registered') === 'true'
  const initialEmail = searchParams.get('email') || ''

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginFormData>({
    resolver: zodResolver(loginSchema),
    defaultValues: {
      email: initialEmail,
      password: '',
    },
  })

  const onSubmit = async (data: LoginFormData) => {
    try {
      setServerError(null)
      const res = await authApi.login(data)
      setAuthToken(res.accessToken)
      navigate('/', { replace: true })
    } catch (err: unknown) {
      if (err instanceof Error) {
        setServerError(err.message)
      } else {
        setServerError('An unexpected error occurred during login.')
      }
    }
  }

  return (
    <Card className="shadow-lg border-border/80">
      <CardHeader className="space-y-1.5 pb-5">
        <div className="flex items-center justify-between">
          <CardTitle className="text-xl font-bold tracking-tight">Sign in</CardTitle>
          <div className="p-1.5 rounded-md bg-secondary text-secondary-foreground">
            <LogIn className="h-4 w-4" />
          </div>
        </div>
        <CardDescription>
          Enter your credentials to access your issue tracker workspace
        </CardDescription>
      </CardHeader>

      <form onSubmit={handleSubmit(onSubmit)}>
        <CardContent className="space-y-4">
          {isRegisteredSuccess && (
            <Alert variant="success">
              Registration successful! Please sign in with your new credentials.
            </Alert>
          )}

          {serverError && (
            <Alert variant="destructive">
              {serverError}
            </Alert>
          )}

          <div className="space-y-1.5">
            <Label htmlFor="email">Email address</Label>
            <Input
              id="email"
              type="email"
              placeholder="developer@example.com"
              autoComplete="email"
              autoFocus={!initialEmail}
              error={!!errors.email}
              {...register('email')}
            />
            {errors.email && (
              <p className="text-xs text-destructive">{errors.email.message}</p>
            )}
          </div>

          <div className="space-y-1.5">
            <div className="flex items-center justify-between">
              <Label htmlFor="password">Password</Label>
            </div>
            <div className="relative">
              <Input
                id="password"
                type={showPassword ? 'text' : 'password'}
                placeholder="••••••••"
                autoComplete="current-password"
                autoFocus={!!initialEmail}
                error={!!errors.password}
                className="pr-10"
                {...register('password')}
              />
              <button
                type="button"
                onClick={() => setShowPassword(prev => !prev)}
                className="absolute right-2.5 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground transition-colors p-1"
                aria-label={showPassword ? 'Hide password' : 'Show password'}
              >
                {showPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
              </button>
            </div>
            {errors.password && (
              <p className="text-xs text-destructive">{errors.password.message}</p>
            )}
          </div>
        </CardContent>

        <CardFooter className="flex flex-col space-y-4 pt-2">
          <Button
            type="submit"
            className="w-full font-medium"
            isLoading={isSubmitting}
          >
            {isSubmitting ? 'Authenticating...' : 'Sign in'}
          </Button>

          <p className="text-center text-xs text-muted-foreground">
            Don't have an account?{' '}
            <Link
              to="/register"
              className="font-medium text-foreground underline-offset-4 hover:underline"
            >
              Create an account
            </Link>
          </p>
        </CardFooter>
      </form>
    </Card>
  )
}
