import React from 'react'
import { Outlet } from 'react-router-dom'
import { ThemeToggle } from '@/shared/components/ui/ThemeToggle'
import { Layers } from 'lucide-react'

export const AuthLayout: React.FC = () => {
  return (
    <div className="min-h-screen flex flex-col justify-between bg-background text-foreground transition-colors duration-200">
      {/* Top Bar */}
      <header className="w-full flex items-center justify-between px-6 py-4 border-b border-border/40 backdrop-blur-sm">
        <div className="flex items-center gap-2.5">
          <div className="h-8 w-8 rounded-lg bg-primary text-primary-foreground flex items-center justify-center font-bold text-sm shadow-sm">
            <Layers className="h-4 w-4" />
          </div>
          <div className="flex flex-col">
            <span className="font-semibold text-sm tracking-tight leading-none">Issue Tracker</span>
            <span className="text-[11px] text-muted-foreground leading-tight mt-0.5">Distributed Monolith</span>
          </div>
        </div>
        <div className="flex items-center gap-2">
          <ThemeToggle />
        </div>
      </header>

      {/* Center Auth Card Form */}
      <main className="flex-1 flex items-center justify-center p-4 sm:p-6 my-8">
        <div className="w-full max-w-md">
          <Outlet />
        </div>
      </main>

      {/* Footer */}
      <footer className="w-full py-4 px-6 border-t border-border/40 text-center text-xs text-muted-foreground flex flex-col sm:flex-row items-center justify-between gap-2">
        <span>Distributed Issue Tracker</span>
        <span>Secure In-Memory JWT Architecture</span>
      </footer>
    </div>
  )
}
