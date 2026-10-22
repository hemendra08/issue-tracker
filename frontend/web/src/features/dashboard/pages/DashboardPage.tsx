import React, { useState } from 'react'
import { Link } from 'react-router-dom'
import { useAuth } from '@/features/auth/hooks/useAuth'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/shared/components/ui/Card'
import { Button } from '@/shared/components/ui/Button'
import { 
  ShieldCheck, 
  Copy, 
  Check, 
  FolderGit2, 
  CircleDot, 
  RefreshCw, 
  KeyRound, 
  User, 
  Calendar, 
  Mail,
  Server
} from 'lucide-react'
import { useQueryClient } from '@tanstack/react-query'

export const DashboardPage: React.FC = () => {
  const { user, token } = useAuth()
  const queryClient = useQueryClient()
  const [copiedId, setCopiedId] = useState(false)
  const [isRefreshing, setIsRefreshing] = useState(false)

  const handleCopyId = () => {
    if (user?.id) {
      navigator.clipboard.writeText(user.id)
      setCopiedId(true)
      setTimeout(() => setCopiedId(false), 2000)
    }
  }

  const handleRefreshIdentity = async () => {
    setIsRefreshing(true)
    await queryClient.invalidateQueries({ queryKey: ['auth', 'me'] })
    setTimeout(() => setIsRefreshing(false), 500)
  }

  const formatDate = (dateStr?: string) => {
    if (!dateStr) return 'N/A'
    try {
      return new Intl.DateTimeFormat('en-US', {
        dateStyle: 'medium',
        timeStyle: 'short',
      }).format(new Date(dateStr))
    } catch {
      return dateStr
    }
  }

  return (
    <div className="max-w-6xl mx-auto space-y-8 animate-in fade-in duration-300">
      {/* Welcome Hero */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-2 border-b border-border/60">
        <div>
          <h1 className="text-2xl sm:text-3xl font-bold tracking-tight text-foreground">
            Welcome back, {user?.displayName || 'Developer'}
          </h1>
          <p className="text-sm text-muted-foreground mt-1">
            Distributed Issue Tracker
          </p>
        </div>

        <div className="flex items-center gap-2">
          <Button
            variant="outline"
            size="sm"
            onClick={handleRefreshIdentity}
            disabled={isRefreshing}
            className="text-xs"
          >
            <RefreshCw className={`mr-1.5 h-3.5 w-3.5 ${isRefreshing ? 'animate-spin' : ''}`} />
            Refresh Session
          </Button>
        </div>
      </div>

      {/* Grid: Identity Card & Session Architecture */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        {/* Profile / Identity Information */}
        <Card className="shadow-sm">
          <CardHeader className="pb-4">
            <div className="flex items-center justify-between">
              <CardTitle className="text-base font-semibold flex items-center gap-2">
                <User className="h-4 w-4 text-primary" />
                Authenticated User Profile
              </CardTitle>
              <span className="text-[11px] font-mono px-2 py-0.5 rounded bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 font-medium">
                HTTP 200 OK
              </span>
            </div>
            <CardDescription>
              Verified claims fetched directly from <code className="text-xs font-mono bg-muted px-1 py-0.5 rounded">/api/v1/auth/me</code>
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="space-y-1">
              <span className="text-xs font-medium text-muted-foreground">User ID (Guid)</span>
              <div className="flex items-center justify-between p-2 rounded-md bg-muted/60 font-mono text-xs text-foreground">
                <span className="truncate">{user?.id}</span>
                <button
                  type="button"
                  onClick={handleCopyId}
                  className="ml-2 p-1 text-muted-foreground hover:text-foreground rounded transition-colors"
                  title="Copy User ID"
                >
                  {copiedId ? <Check className="h-3.5 w-3.5 text-emerald-500" /> : <Copy className="h-3.5 w-3.5" />}
                </button>
              </div>
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div className="space-y-1">
                <span className="text-xs font-medium text-muted-foreground flex items-center gap-1.5">
                  <User className="h-3.5 w-3.5" />
                  Display Name
                </span>
                <p className="text-sm font-medium text-foreground">{user?.displayName}</p>
              </div>

              <div className="space-y-1">
                <span className="text-xs font-medium text-muted-foreground flex items-center gap-1.5">
                  <Mail className="h-3.5 w-3.5" />
                  Email Address
                </span>
                <p className="text-sm font-medium text-foreground truncate">{user?.email}</p>
              </div>
            </div>

            <div className="space-y-1 pt-1 border-t border-border/50">
              <span className="text-xs font-medium text-muted-foreground flex items-center gap-1.5">
                <Calendar className="h-3.5 w-3.5" />
                Account Created (UTC)
              </span>
              <p className="text-xs text-foreground font-mono">{formatDate(user?.createdAtUtc)}</p>
            </div>
          </CardContent>
        </Card>

        {/* Security & Token Architecture */}
        <Card className="shadow-sm">
          <CardHeader className="pb-4">
            <div className="flex items-center justify-between">
              <CardTitle className="text-base font-semibold flex items-center gap-2">
                <ShieldCheck className="h-4 w-4 text-emerald-500" />
                Security & Session Context
              </CardTitle>
              <span className="text-[11px] font-mono px-2 py-0.5 rounded bg-primary/10 text-primary font-medium">
                JWT Bearer
              </span>
            </div>
            <CardDescription>
              Architectural implementation details
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-3.5 text-xs text-muted-foreground">
            <div className="flex items-start gap-2.5 p-3 rounded-md bg-muted/40">
              <KeyRound className="h-4 w-4 text-primary shrink-0 mt-0.5" />
              <div>
                <p className="font-semibold text-foreground">In-Memory Token Strategy</p>
                <p className="mt-0.5 text-muted-foreground">
                  Access tokens are held in active React memory only and never saved to localStorage, mitigating XSS token theft.
                </p>
              </div>
            </div>

            <div className="flex items-start gap-2.5 p-3 rounded-md bg-muted/40">
              <Server className="h-4 w-4 text-primary shrink-0 mt-0.5" />
              <div>
                <p className="font-semibold text-foreground">Feature-Wise Modular Monolith</p>
                <p className="mt-0.5 text-muted-foreground">
                  The backend hosts the Auth feature with decoupled boundaries, prepared for extraction into an independent microservice.
                </p>
              </div>
            </div>

            <div className="space-y-1 pt-1">
              <span className="font-medium text-muted-foreground">Access Token Status</span>
              <p className="font-mono text-[11px] text-foreground truncate bg-muted/60 p-2 rounded-md">
                {token ? `${token.slice(0, 24)}••••••••${token.slice(-16)}` : 'No token active'}
              </p>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Active & Upcoming Workspaces */}
      <div className="space-y-3">
        <h2 className="text-lg font-semibold tracking-tight text-foreground">Workspaces & Roadmaps</h2>
        
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <Link
            to="/projects"
            className="group p-5 rounded-xl border border-border bg-card hover:border-primary/50 transition-all flex flex-col justify-between space-y-3 shadow-xs"
          >
            <div className="space-y-1.5">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <FolderGit2 className="h-5 w-5 text-primary" />
                  <h3 className="font-semibold text-sm text-foreground group-hover:text-primary transition-colors">
                    Projects Workspace
                  </h3>
                </div>
                <span className="text-[10px] px-2 py-0.5 rounded-full bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 font-medium border border-emerald-500/20">
                  Active
                </span>
              </div>
              <p className="text-xs text-muted-foreground">
                Collaborative project creation, member role management (Owner, Admin, Member), and archive lifecycle.
              </p>
            </div>
            <p className="text-xs font-medium text-primary flex items-center gap-1">
              Open Projects Workspace &rarr;
            </p>
          </Link>

          <div className="p-5 rounded-xl border border-dashed border-border bg-card/40 flex flex-col justify-between space-y-3">
            <div className="space-y-1.5">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <CircleDot className="h-5 w-5 text-muted-foreground" />
                  <h3 className="font-medium text-sm text-foreground">Issues & Triage Board</h3>
                </div>
                <span className="text-[10px] px-1.5 py-0.5 rounded bg-muted text-muted-foreground border border-border">
                  Upcoming
                </span>
              </div>
              <p className="text-xs text-muted-foreground">
                Issue lifecycle management, states, priority, assignees, and real-time distributed updates.
              </p>
            </div>
            <p className="text-[11px] font-medium text-muted-foreground">Coming soon</p>
          </div>
        </div>
      </div>
    </div>
  )
}
