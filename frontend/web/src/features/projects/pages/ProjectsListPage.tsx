import React, { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useProjects, useCreateProject } from '../hooks/useProjects'
import { createProjectSchema, CreateProjectFormData } from '../schemas/projectSchemas'
import { Button } from '@/shared/components/ui/Button'
import { Input } from '@/shared/components/ui/Input'
import { Label } from '@/shared/components/ui/Label'
import { Alert } from '@/shared/components/ui/Alert'
import { Card } from '@/shared/components/ui/Card'
import { Modal } from '@/shared/components/ui/Modal'
import { Skeleton } from '@/shared/components/ui/Skeleton'
import { 
  Plus, 
  Search, 
  FolderGit2, 
  Users, 
  Archive, 
  ShieldAlert, 
  Crown, 
  ShieldCheck, 
  UserCheck 
} from 'lucide-react'

export const ProjectsListPage: React.FC = () => {
  const navigate = useNavigate()
  const [search, setSearch] = useState('')
  const [filter, setFilter] = useState<'active' | 'archived' | 'all'>('active')
  const [isCreateOpen, setIsCreateOpen] = useState(false)
  const [createError, setCreateError] = useState<string | null>(null)

  const includeArchived = filter === 'all' ? true : filter === 'archived' ? true : false

  const { data: pagedData, isLoading, error } = useProjects({
    search: search.trim() || undefined,
    includeArchived,
  })

  const createMutation = useCreateProject()

  const {
    register,
    handleSubmit,
    reset,
    setValue,
    formState: { errors, isSubmitting },
  } = useForm<CreateProjectFormData>({
    resolver: zodResolver(createProjectSchema),
    defaultValues: {
      name: '',
      key: '',
      description: '',
    },
  })

  // Auto-generate suggested key when name changes
  const handleNameChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const name = e.target.value
    // If key has not been manually touched or is empty, suggest 3-letter acronym
    const suggestedKey = name
      .replace(/[^a-zA-Z0-9\s]/g, '')
      .split(/\s+/)
      .filter(Boolean)
      .map(w => w[0])
      .join('')
      .toUpperCase()
      .slice(0, 5)

    if (suggestedKey.length >= 2) {
      setValue('key', suggestedKey)
    }
  }

  const onCreateSubmit = async (data: CreateProjectFormData) => {
    try {
      setCreateError(null)
      const project = await createMutation.mutateAsync({
        name: data.name,
        key: data.key.toUpperCase(),
        description: data.description || undefined,
      })
      reset()
      setIsCreateOpen(false)
      navigate(`/projects/${project.id}`)
    } catch (err: any) {
      setCreateError(err.message || 'Failed to create project.')
    }
  }

  // Filter items in memory if filter is explicitly 'archived'
  const filteredProjects = pagedData?.items.filter(p => {
    if (filter === 'active') return !p.isArchived
    if (filter === 'archived') return p.isArchived
    return true
  }) ?? []

  const getRoleBadge = (role: string) => {
    switch (role) {
      case 'Owner':
        return (
          <span className="inline-flex items-center gap-1 text-[11px] font-semibold px-2 py-0.5 rounded-full bg-amber-500/10 text-amber-600 dark:text-amber-400 border border-amber-500/20">
            <Crown className="h-3 w-3" />
            Owner
          </span>
        )
      case 'Admin':
        return (
          <span className="inline-flex items-center gap-1 text-[11px] font-semibold px-2 py-0.5 rounded-full bg-purple-500/10 text-purple-600 dark:text-purple-400 border border-purple-500/20">
            <ShieldCheck className="h-3 w-3" />
            Admin
          </span>
        )
      default:
        return (
          <span className="inline-flex items-center gap-1 text-[11px] font-medium px-2 py-0.5 rounded-full bg-muted text-muted-foreground border border-border">
            <UserCheck className="h-3 w-3" />
            Member
          </span>
        )
    }
  }

  return (
    <div className="max-w-6xl mx-auto space-y-6 animate-in fade-in duration-200">
      {/* Page Title & Action Bar */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-2 border-b border-border/60">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-foreground flex items-center gap-2.5">
            <FolderGit2 className="h-6 w-6 text-primary" />
            Projects
          </h1>
          <p className="text-xs text-muted-foreground mt-0.5">
            Manage your issue tracking workspaces, members, and access roles
          </p>
        </div>

        <Button onClick={() => setIsCreateOpen(true)} className="text-sm shadow-sm">
          <Plus className="mr-1.5 h-4 w-4" />
          New Project
        </Button>
      </div>

      {/* Filters & Search Toolbar */}
      <div className="flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-3">
        {/* Search Input */}
        <div className="relative flex-1 max-w-md">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground pointer-events-none" />
          <Input
            value={search}
            onChange={e => setSearch(e.target.value)}
            placeholder="Search projects by name or key..."
            className="pl-9 text-sm"
          />
        </div>

        {/* Tab Filters */}
        <div className="inline-flex p-1 rounded-lg bg-muted/60 border border-border text-xs font-medium shrink-0">
          <button
            type="button"
            onClick={() => setFilter('active')}
            className={`px-3 py-1 rounded-md transition-colors cursor-pointer ${
              filter === 'active'
                ? 'bg-background text-foreground shadow-xs font-semibold'
                : 'text-muted-foreground hover:text-foreground'
            }`}
          >
            Active
          </button>
          <button
            type="button"
            onClick={() => setFilter('all')}
            className={`px-3 py-1 rounded-md transition-colors cursor-pointer ${
              filter === 'all'
                ? 'bg-background text-foreground shadow-xs font-semibold'
                : 'text-muted-foreground hover:text-foreground'
            }`}
          >
            All
          </button>
          <button
            type="button"
            onClick={() => setFilter('archived')}
            className={`px-3 py-1 rounded-md transition-colors cursor-pointer ${
              filter === 'archived'
                ? 'bg-background text-foreground shadow-xs font-semibold'
                : 'text-muted-foreground hover:text-foreground'
            }`}
          >
            Archived
          </button>
        </div>
      </div>

      {/* Error Banner */}
      {error && (
        <Alert variant="destructive">
          <ShieldAlert className="h-4 w-4" />
          <span>Failed to load projects: {(error as any)?.message || 'Unknown error'}</span>
        </Alert>
      )}

      {/* Projects Grid / List */}
      {isLoading ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {[1, 2, 3, 4, 5, 6].map(i => (
            <Card key={i} className="p-5 space-y-4">
              <div className="flex items-center justify-between">
                <Skeleton className="h-6 w-16" />
                <Skeleton className="h-5 w-20 rounded-full" />
              </div>
              <Skeleton className="h-5 w-3/4" />
              <Skeleton className="h-10 w-full" />
              <div className="flex items-center justify-between pt-2 border-t border-border/40">
                <Skeleton className="h-4 w-24" />
                <Skeleton className="h-4 w-16" />
              </div>
            </Card>
          ))}
        </div>
      ) : filteredProjects.length === 0 ? (
        <div className="text-center py-16 px-4 rounded-xl border border-dashed border-border bg-card/40 space-y-3">
          <div className="h-12 w-12 rounded-full bg-muted flex items-center justify-center mx-auto text-muted-foreground">
            <FolderGit2 className="h-6 w-6" />
          </div>
          <h3 className="font-semibold text-base text-foreground">
            {search ? 'No projects match your search' : 'No projects found'}
          </h3>
          <p className="text-xs text-muted-foreground max-w-sm mx-auto">
            {search
              ? 'Try adjusting your search query or switching filters.'
              : 'Create a project to start organizing workspaces and collaborating with team members.'}
          </p>
          {!search && (
            <div className="pt-2">
              <Button onClick={() => setIsCreateOpen(true)} size="sm">
                <Plus className="mr-1.5 h-3.5 w-3.5" />
                Create your first project
              </Button>
            </div>
          )}
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {filteredProjects.map(project => (
            <Link
              key={project.id}
              to={`/projects/${project.id}`}
              className="group block rounded-xl border border-border bg-card p-5 text-card-foreground shadow-xs hover:border-primary/50 hover:shadow-md transition-all duration-150"
            >
              <div className="flex items-start justify-between gap-2 pb-2">
                <span className="font-mono text-xs font-bold px-2 py-0.5 rounded bg-primary/10 text-primary border border-primary/20">
                  {project.key}
                </span>
                <div className="flex items-center gap-1.5">
                  {project.isArchived && (
                    <span className="inline-flex items-center gap-1 text-[10px] font-medium px-2 py-0.5 rounded-full bg-zinc-500/10 text-zinc-500 dark:text-zinc-400 border border-zinc-500/20">
                      <Archive className="h-3 w-3" />
                      Archived
                    </span>
                  )}
                  {getRoleBadge(project.currentUserRole)}
                </div>
              </div>

              <h2 className="font-semibold text-base text-foreground group-hover:text-primary transition-colors truncate">
                {project.name}
              </h2>

              <p className="text-xs text-muted-foreground line-clamp-2 mt-1 min-h-[2.5rem]">
                {project.description || 'No description provided.'}
              </p>

              <div className="flex items-center justify-between pt-3 mt-3 border-t border-border/50 text-[11px] text-muted-foreground">
                <span className="flex items-center gap-1.5">
                  <Users className="h-3.5 w-3.5" />
                  {project.memberCount} {project.memberCount === 1 ? 'member' : 'members'}
                </span>
                <span>
                  Updated {new Date(project.updatedAtUtc).toLocaleDateString()}
                </span>
              </div>
            </Link>
          ))}
        </div>
      )}

      {/* Create Project Modal */}
      <Modal
        isOpen={isCreateOpen}
        onClose={() => {
          setIsCreateOpen(false)
          reset()
          setCreateError(null)
        }}
        title="Create new project"
        description="Set up a new workspace for tracking issues and collaborating"
      >
        <form onSubmit={handleSubmit(onCreateSubmit)} className="space-y-4">
          {createError && (
            <Alert variant="destructive">
              {createError}
            </Alert>
          )}

          <div className="space-y-1.5">
            <Label htmlFor="create-name">Project Name</Label>
            <Input
              id="create-name"
              placeholder="e.g. Core Platform Engine"
              autoFocus
              error={!!errors.name}
              {...register('name', { onChange: handleNameChange })}
            />
            {errors.name && (
              <p className="text-xs text-destructive">{errors.name.message}</p>
            )}
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="create-key">Project Key</Label>
            <Input
              id="create-key"
              placeholder="e.g. CORE, ENG, WEB"
              maxLength={10}
              className="font-mono uppercase tracking-wider"
              error={!!errors.key}
              {...register('key')}
            />
            {errors.key ? (
              <p className="text-xs text-destructive">{errors.key.message}</p>
            ) : (
              <p className="text-[11px] text-muted-foreground">
                2-10 uppercase alphanumeric characters used as prefix for issue identifiers
              </p>
            )}
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="create-desc">Description (Optional)</Label>
            <textarea
              id="create-desc"
              rows={3}
              placeholder="Brief description of this project's purpose..."
              className="flex w-full rounded-md border border-input bg-transparent px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
              {...register('description')}
            />
            {errors.description && (
              <p className="text-xs text-destructive">{errors.description.message}</p>
            )}
          </div>

          <div className="flex items-center justify-end gap-2 pt-2 border-t border-border/50">
            <Button
              type="button"
              variant="outline"
              onClick={() => {
                setIsCreateOpen(false)
                reset()
                setCreateError(null)
              }}
            >
              Cancel
            </Button>
            <Button type="submit" isLoading={isSubmitting}>
              Create Project
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  )
}
