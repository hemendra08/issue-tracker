import React, { useState } from 'react'
import { Outlet, Link, useLocation } from 'react-router-dom'
import { useAuth } from '@/features/auth/hooks/useAuth'
import { ThemeToggle } from '@/shared/components/ui/ThemeToggle'
import { 
  Layers, 
  LayoutDashboard, 
  FolderGit2, 
  CircleDot, 
  LogOut, 
  CheckCircle2, 
  Menu, 
  X,
  User as UserIcon 
} from 'lucide-react'

export const AppLayout: React.FC = () => {
  const { user, logout } = useAuth()
  const location = useLocation()
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false)

  // Get user initials for avatar
  const getInitials = (name?: string) => {
    if (!name) return 'U'
    const parts = name.trim().split(' ')
    if (parts.length >= 2) {
      return `${parts[0][0]}${parts[1][0]}`.toUpperCase()
    }
    return name.slice(0, 2).toUpperCase()
  }

  const navItems = [
    {
      label: 'Overview',
      path: '/',
      icon: LayoutDashboard,
      current: location.pathname === '/',
    },
    {
      label: 'Projects',
      path: '/projects',
      icon: FolderGit2,
      current: location.pathname.startsWith('/projects'),
    },
    {
      label: 'Issues',
      path: '#',
      icon: CircleDot,
      badge: 'Soon',
      current: false,
    },
  ]

  return (
    <div className="min-h-screen flex bg-background text-foreground transition-colors duration-200">
      {/* Sidebar - Desktop */}
      <aside className="hidden md:flex flex-col w-64 border-r border-border bg-card shrink-0 select-none">
        {/* Workspace Brand Header */}
        <div className="h-16 px-5 border-b border-border flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="h-8 w-8 rounded-lg bg-primary text-primary-foreground flex items-center justify-center font-bold text-sm shadow-sm">
              <Layers className="h-4 w-4" />
            </div>
            <div>
              <h1 className="font-semibold text-sm tracking-tight leading-none text-foreground">Issue Tracker</h1>
              <p className="text-[11px] text-muted-foreground mt-0.5">Distributed Monolith</p>
            </div>
          </div>
        </div>

        {/* Navigation */}
        <div className="flex-1 py-4 px-3 space-y-1">
          <div className="px-3 pb-2 text-[10px] font-semibold uppercase tracking-wider text-muted-foreground">
            Workspace
          </div>
          {navItems.map((item, idx) => {
            const Icon = item.icon
            return (
              <div key={idx}>
                {item.path !== '#' ? (
                  <Link
                    to={item.path}
                    className={`flex items-center justify-between px-3 py-2 text-sm font-medium rounded-md transition-colors ${
                      item.current
                        ? 'bg-secondary text-secondary-foreground font-semibold'
                        : 'text-muted-foreground hover:bg-muted hover:text-foreground'
                    }`}
                  >
                    <div className="flex items-center gap-3">
                      <Icon className="h-4 w-4" />
                      <span>{item.label}</span>
                    </div>
                  </Link>
                ) : (
                  <div
                    className="flex items-center justify-between px-3 py-2 text-sm font-medium rounded-md text-muted-foreground hover:bg-muted/50 cursor-not-allowed opacity-75"
                    title="Coming soon"
                  >
                    <div className="flex items-center gap-3">
                      <Icon className="h-4 w-4" />
                      <span>{item.label}</span>
                    </div>
                    {item.badge && (
                      <span className="text-[10px] font-medium px-1.5 py-0.5 rounded bg-muted text-muted-foreground border border-border/50">
                        {item.badge}
                      </span>
                    )}
                  </div>
                )}
              </div>
            )
          })}
        </div>

        {/* User Identity Section */}
        <div className="p-3 border-t border-border">
          <div className="flex items-center justify-between p-2 rounded-lg bg-muted/40 hover:bg-muted/70 transition-colors">
            <div className="flex items-center gap-3 min-w-0">
              <div className="h-9 w-9 rounded-full bg-primary/10 text-primary font-semibold text-xs flex items-center justify-center border border-primary/20 shrink-0">
                {getInitials(user?.displayName)}
              </div>
              <div className="min-w-0 flex-1">
                <p className="text-xs font-medium text-foreground truncate">{user?.displayName || 'User'}</p>
                <p className="text-[11px] text-muted-foreground truncate">{user?.email}</p>
              </div>
            </div>
            <button
              onClick={logout}
              title="Sign out"
              className="p-1.5 rounded-md text-muted-foreground hover:text-destructive hover:bg-destructive/10 transition-colors cursor-pointer"
            >
              <LogOut className="h-4 w-4" />
            </button>
          </div>
        </div>
      </aside>

      {/* Main Content Area */}
      <div className="flex-1 flex flex-col min-w-0">
        {/* Top Header */}
        <header className="h-16 px-4 md:px-6 border-b border-border bg-card/60 backdrop-blur-sm flex items-center justify-between sticky top-0 z-10">
          <div className="flex items-center gap-3">
            <button
              type="button"
              onClick={() => setMobileMenuOpen(prev => !prev)}
              className="md:hidden p-2 rounded-md hover:bg-muted text-foreground"
            >
              {mobileMenuOpen ? <X className="h-5 w-5" /> : <Menu className="h-5 w-5" />}
            </button>
            <div className="flex items-center gap-2 text-sm text-muted-foreground">
              <span className="hidden sm:inline">Workspace</span>
              <span className="hidden sm:inline">/</span>
              <span className="font-medium text-foreground">Overview</span>
            </div>
          </div>

          <div className="flex items-center gap-3">
            {/* Backend API Connection Status */}
            <div className="hidden sm:flex items-center gap-1.5 px-2.5 py-1 rounded-full bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 text-xs font-medium border border-emerald-500/20">
              <CheckCircle2 className="h-3.5 w-3.5" />
              <span>Auth API Connected</span>
            </div>

            <ThemeToggle />

            <div className="h-4 w-px bg-border" />

            <button
              onClick={logout}
              className="flex items-center gap-1.5 text-xs text-muted-foreground hover:text-foreground font-medium transition-colors px-2 py-1.5 rounded-md hover:bg-muted cursor-pointer"
            >
              <LogOut className="h-3.5 w-3.5" />
              <span className="hidden sm:inline">Sign out</span>
            </button>
          </div>
        </header>

        {/* Mobile menu dropdown */}
        {mobileMenuOpen && (
          <div className="md:hidden p-4 border-b border-border bg-card space-y-3">
            <div className="flex items-center gap-3 p-2 rounded-md bg-muted/50">
              <UserIcon className="h-5 w-5 text-muted-foreground" />
              <div className="min-w-0 flex-1">
                <p className="text-sm font-medium text-foreground truncate">{user?.displayName}</p>
                <p className="text-xs text-muted-foreground truncate">{user?.email}</p>
              </div>
            </div>
            <button
              onClick={logout}
              className="w-full flex items-center justify-center gap-2 p-2 rounded-md text-sm text-destructive hover:bg-destructive/10"
            >
              <LogOut className="h-4 w-4" />
              <span>Sign out</span>
            </button>
          </div>
        )}

        {/* Main Viewport */}
        <main className="flex-1 p-4 md:p-8 overflow-y-auto">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
