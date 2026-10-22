import React from 'react'
import { cn } from '@/shared/lib/utils'
import { AlertCircle, CheckCircle2, Info } from 'lucide-react'

export interface AlertProps extends React.HTMLAttributes<HTMLDivElement> {
  variant?: 'default' | 'destructive' | 'success'
}

export const Alert = React.forwardRef<HTMLDivElement, AlertProps>(
  ({ className, variant = 'default', children, ...props }, ref) => {
    const variantStyles = {
      default: 'bg-muted text-foreground border-border',
      destructive: 'border-destructive/50 bg-destructive/10 text-destructive dark:border-destructive [&>svg]:text-destructive',
      success: 'border-emerald-500/50 bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 [&>svg]:text-emerald-500',
    }

    const Icon = variant === 'destructive' ? AlertCircle : variant === 'success' ? CheckCircle2 : Info

    return (
      <div
        ref={ref}
        role="alert"
        className={cn(
          'relative w-full rounded-lg border p-3.5 text-sm flex items-start gap-3',
          variantStyles[variant],
          className
        )}
        {...props}
      >
        <Icon className="h-5 w-5 shrink-0 mt-0.5" />
        <div className="flex-1 text-sm leading-relaxed">{children}</div>
      </div>
    )
  }
)
Alert.displayName = 'Alert'
