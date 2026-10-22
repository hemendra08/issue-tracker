import React from 'react'
import { useParams, Link, useNavigate } from 'react-router-dom'
import { useProject } from '../hooks/useProjects'
import { Button } from '@/shared/components/ui/Button'
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/shared/components/ui/Card'
import { Alert } from '@/shared/components/ui/Alert'
import { Skeleton } from '@/shared/components/ui/Skeleton'
import { 
  ArrowLeft, 
  Archive, 
  Users, 
  Settings, 
  User, 
  Calendar, 
  Crown, 
  ShieldCheck, 
  UserCheck, 
  ExternalLink,
  ShieldAlert
} from 'lucide-react'

export const ProjectDetailPage: React.FC = () => {
  const { projectId } = useParams<{ projectId: string }>()
  const navigate = useNavigate()

  const { data: project, isLoading, error } = useProject(projectId)

  if (isLoading) {
    return (
      <div className="max-w-5xl mx-auto space-y-6">
        <Skeleton className="h-6 w-32" />
        <Skeleton className="h-10 w-64" />
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          <Skeleton className="h-48 col-span-2" />
          <Skeleton className="h-48" />
        </div>
      </div>
    )
  }

  if (error || !project) {
    return (
      <div className="max-w-md mx-auto text-center py-16 space-y-4">
        <div className="h-12 w-12 rounded-full bg-destructive/10 text-destructive flex items-center justify-center mx-auto">
          <ShieldAlert className="h-6 w-6" />
        </div>
        <h2 className="text-lg font-semibold text-foreground">Project Not Accessible</h2>
        <p className="text-xs text-muted-foreground">
          This project does not exist, or your account does not have permission to view it.
        </p>
        <Button onClick={() => navigate('/projects')} variant="outline" size="sm">
          <ArrowLeft className="mr-1.5 h-4 w-4" />
          Return to Projects
        </Button>
      </div>
    )
  }

  const isOwner = project.currentUserRole === 'Owner'
  const isAdmin = project.currentUserRole === 'Admin'
  const canManage = isOwner || isAdmin

  const getRoleBadge = (role: string) => {
    switch (role) {
      case 'Owner':
        return (
          <span className="inline-flex items-center gap-1 text-xs font-semibold px-2.5 py-1 rounded-full bg-amber-500/10 text-amber-600 dark:text-amber-400 border border-amber-500/20">
            <Crown className="h-3.5 w-3.5" />
            Owner
          </span>
        )
      case 'Admin':
        return (
          <span className="inline-flex items-center gap-1 text-xs font-semibold px-2.5 py-1 rounded-full bg-purple-500/10 text-purple-600 dark:text-purple-400 border border-purple-500/20">
            <ShieldCheck className="h-3.5 w-3.5" />
            Admin
          </span>
        )
      default:
        return (
          <span className="inline-flex items-center gap-1 text-xs font-medium px-2.5 py-1 rounded-full bg-muted text-muted-foreground border border-border">
            <UserCheck className="h-3.5 w-3.5" />
            Member
          </span>
        )
    }
  }

  return (
    <div className="max-w-5xl mx-auto space-y-6 animate-in fade-in duration-200">
      {/* Breadcrumb Navigation */}
      <div className="flex items-center gap-2 text-xs text-muted-foreground">
        <Link to="/projects" className="hover:text-foreground transition-colors flex items-center gap-1">
          <ArrowLeft className="h-3.5 w-3.5" />
          Projects
        </Link>
        <span>/</span>
        <span className="font-mono text-foreground">{project.key}</span>
      </div>

      {/* Project Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-4 border-b border-border">
        <div className="space-y-1.5">
          <div className="flex items-center gap-2.5 flex-wrap">
            <span className="font-mono text-xs font-bold px-2 py-0.5 rounded bg-primary/10 text-primary border border-primary/20">
              {project.key}
            </span>
            <h1 className="text-2xl font-bold tracking-tight text-foreground">{project.name}</h1>
            {project.isArchived && (
              <span className="inline-flex items-center gap-1 text-xs font-medium px-2.5 py-0.5 rounded-full bg-zinc-500/10 text-zinc-500 dark:text-zinc-400 border border-zinc-500/20">
                <Archive className="h-3.5 w-3.5" />
                Archived
              </span>
            )}
            {getRoleBadge(project.currentUserRole)}
          </div>
          <p className="text-xs text-muted-foreground">
            Created {new Date(project.createdAtUtc).toLocaleDateString()} • Owned by {project.ownerName || 'Project Owner'}
          </p>
        </div>

        {/* Tab Quick Links */}
        <div className="flex items-center gap-2">
          <Link to={`/projects/${project.id}/members`}>
            <Button variant="outline" size="sm">
              <Users className="mr-1.5 h-3.5 w-3.5" />
              Members ({project.memberCount})
            </Button>
          </Link>

          {canManage && (
            <Link to={`/projects/${project.id}/settings`}>
              <Button variant="outline" size="sm">
                <Settings className="mr-1.5 h-3.5 w-3.5" />
                Settings
              </Button>
            </Link>
          )}
        </div>
      </div>

      {/* Archived Notice */}
      {project.isArchived && (
        <Alert variant="default">
          <Archive className="h-4 w-4" />
          <span>
            This project is currently archived. All project details and memberships are read-only.
            {isOwner && ' You can restore this project in Project Settings.'}
          </span>
        </Alert>
      )}

      {/* Main Grid Overview */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        {/* Left 2 Cols: Description & Workspace Context */}
        <div className="md:col-span-2 space-y-6">
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-base">About this project</CardTitle>
            </CardHeader>
            <CardContent>
              {project.description ? (
                <p className="text-sm text-foreground leading-relaxed whitespace-pre-wrap">
                  {project.description}
                </p>
              ) : (
                <p className="text-xs text-muted-foreground italic">
                  No description has been provided for this workspace.
                </p>
              )}
            </CardContent>
          </Card>

          {/* Issue Tracker Preview Placeholder (Milestone 4 preview) */}
          <Card className="border-dashed">
            <CardHeader className="pb-2">
              <div className="flex items-center justify-between">
                <CardTitle className="text-sm font-semibold">Issues Board</CardTitle>
                <span className="text-[10px] px-2 py-0.5 rounded bg-muted text-muted-foreground border border-border">
                  Upcoming
                </span>
              </div>
              <CardDescription className="text-xs">
                Issue triage, task assignments, and state transitions will be connected here.
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="p-4 rounded-lg bg-muted/40 text-xs text-muted-foreground text-center">
                Project workspace <span className="font-mono font-medium text-foreground">[{project.key}]</span> is ready for issue tracking.
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Right 1 Col: Metadata & Quick Access */}
        <div className="space-y-6">
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-base">Workspace Information</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3.5 text-xs">
              <div className="flex items-start justify-between pb-2 border-b border-border/50">
                <span className="text-muted-foreground flex items-center gap-1.5">
                  <User className="h-3.5 w-3.5" />
                  Project Owner
                </span>
                <span className="font-medium text-foreground text-right">
                  {project.ownerName || 'Owner'}
                  <span className="block text-[11px] text-muted-foreground">{project.ownerEmail}</span>
                </span>
              </div>

              <div className="flex items-center justify-between pb-2 border-b border-border/50">
                <span className="text-muted-foreground flex items-center gap-1.5">
                  <Users className="h-3.5 w-3.5" />
                  Active Members
                </span>
                <span className="font-medium text-foreground">{project.memberCount}</span>
              </div>

              <div className="flex items-center justify-between pb-2 border-b border-border/50">
                <span className="text-muted-foreground flex items-center gap-1.5">
                  <Calendar className="h-3.5 w-3.5" />
                  Created
                </span>
                <span className="font-medium text-foreground">
                  {new Date(project.createdAtUtc).toLocaleDateString()}
                </span>
              </div>

              <div className="flex items-center justify-between">
                <span className="text-muted-foreground flex items-center gap-1.5">
                  <Calendar className="h-3.5 w-3.5" />
                  Last Updated
                </span>
                <span className="font-medium text-foreground">
                  {new Date(project.updatedAtUtc).toLocaleDateString()}
                </span>
              </div>
            </CardContent>
          </Card>

          {/* Fast Navigation Card */}
          <Card>
            <CardHeader className="pb-2">
              <CardTitle className="text-sm font-semibold">Quick Actions</CardTitle>
            </CardHeader>
            <CardContent className="space-y-2 pt-0">
              <Link
                to={`/projects/${project.id}/members`}
                className="flex items-center justify-between p-2.5 rounded-lg border border-border hover:bg-muted/50 transition-colors text-xs font-medium text-foreground"
              >
                <div className="flex items-center gap-2">
                  <Users className="h-4 w-4 text-primary" />
                  <span>Manage Team Members</span>
                </div>
                <ExternalLink className="h-3 w-3 text-muted-foreground" />
              </Link>

              {canManage && (
                <Link
                  to={`/projects/${project.id}/settings`}
                  className="flex items-center justify-between p-2.5 rounded-lg border border-border hover:bg-muted/50 transition-colors text-xs font-medium text-foreground"
                >
                  <div className="flex items-center gap-2">
                    <Settings className="h-4 w-4 text-primary" />
                    <span>Project Settings</span>
                  </div>
                  <ExternalLink className="h-3 w-3 text-muted-foreground" />
                </Link>
              )}
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  )
}
