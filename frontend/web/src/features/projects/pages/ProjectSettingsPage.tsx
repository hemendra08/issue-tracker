import React, { useState } from 'react'
import { useParams, Link, useNavigate } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useProject, useUpdateProject, useArchiveProject, useRestoreProject } from '../hooks/useProjects'
import { updateProjectSchema, UpdateProjectFormData } from '../schemas/projectSchemas'
import { Button } from '@/shared/components/ui/Button'
import { Input } from '@/shared/components/ui/Input'
import { Label } from '@/shared/components/ui/Label'
import { Alert } from '@/shared/components/ui/Alert'
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@/shared/components/ui/Card'
import { Modal } from '@/shared/components/ui/Modal'
import { Skeleton } from '@/shared/components/ui/Skeleton'
import { 
  Settings, 
  Archive, 
  RotateCcw, 
  AlertTriangle, 
  Check, 
  ShieldAlert 
} from 'lucide-react'

export const ProjectSettingsPage: React.FC = () => {
  const { projectId } = useParams<{ projectId: string }>()
  const navigate = useNavigate()

  const { data: project, isLoading } = useProject(projectId)
  const updateMutation = useUpdateProject(projectId!)
  const archiveMutation = useArchiveProject(projectId!)
  const restoreMutation = useRestoreProject(projectId!)

  const [updateSuccess, setUpdateSuccess] = useState(false)
  const [updateError, setUpdateError] = useState<string | null>(null)

  const [isArchiveModalOpen, setIsArchiveModalOpen] = useState(false)
  const [archiveError, setArchiveError] = useState<string | null>(null)

  const [isRestoreModalOpen, setIsRestoreModalOpen] = useState(false)
  const [restoreError, setRestoreError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting, isDirty },
  } = useForm<UpdateProjectFormData>({
    resolver: zodResolver(updateProjectSchema),
    values: {
      name: project?.name || '',
      description: project?.description || '',
    },
  })

  if (isLoading) {
    return (
      <div className="max-w-4xl mx-auto space-y-6">
        <Skeleton className="h-6 w-32" />
        <Skeleton className="h-10 w-64" />
        <Skeleton className="h-48 w-full" />
      </div>
    )
  }

  if (!project) return null

  const isOwner = project.currentUserRole === 'Owner'
  const isAdmin = project.currentUserRole === 'Admin'
  const canEdit = (isOwner || isAdmin) && !project.isArchived

  const onUpdateSubmit = async (data: UpdateProjectFormData) => {
    try {
      setUpdateError(null)
      setUpdateSuccess(false)
      await updateMutation.mutateAsync({
        name: data.name,
        description: data.description || undefined,
      })
      setUpdateSuccess(true)
      setTimeout(() => setUpdateSuccess(false), 3000)
    } catch (err: any) {
      setUpdateError(err.message || 'Failed to update project.')
    }
  }

  const handleArchiveConfirm = async () => {
    try {
      setArchiveError(null)
      await archiveMutation.mutateAsync()
      setIsArchiveModalOpen(false)
      navigate(`/projects/${project.id}`)
    } catch (err: any) {
      setArchiveError(err.message || 'Failed to archive project.')
    }
  }

  const handleRestoreConfirm = async () => {
    try {
      setRestoreError(null)
      await restoreMutation.mutateAsync()
      setIsRestoreModalOpen(false)
      navigate(`/projects/${project.id}`)
    } catch (err: any) {
      setRestoreError(err.message || 'Failed to restore project.')
    }
  }

  return (
    <div className="max-w-4xl mx-auto space-y-8 animate-in fade-in duration-200">
      {/* Breadcrumbs */}
      <div className="flex items-center gap-2 text-xs text-muted-foreground">
        <Link to="/projects" className="hover:text-foreground transition-colors">
          Projects
        </Link>
        <span>/</span>
        <Link to={`/projects/${project.id}`} className="hover:text-foreground transition-colors font-mono">
          {project.key}
        </Link>
        <span>/</span>
        <span className="text-foreground">Settings</span>
      </div>

      {/* Header */}
      <div className="flex items-center justify-between pb-4 border-b border-border">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-foreground flex items-center gap-2.5">
            <Settings className="h-6 w-6 text-primary" />
            Project Settings
          </h1>
          <p className="text-xs text-muted-foreground mt-0.5">
            Configure metadata, workspace status, and administrative options
          </p>
        </div>
      </div>

      {/* General Settings Card */}
      <Card>
        <CardHeader>
          <CardTitle className="text-lg">General Details</CardTitle>
          <CardDescription>
            Update project identifier details and collaborative description
          </CardDescription>
        </CardHeader>
        <form onSubmit={handleSubmit(onUpdateSubmit)}>
          <CardContent className="space-y-4">
            {updateSuccess && (
              <Alert variant="success">
                <Check className="h-4 w-4" />
                <span>Project details updated successfully.</span>
              </Alert>
            )}

            {updateError && (
              <Alert variant="destructive">
                <ShieldAlert className="h-4 w-4" />
                <span>{updateError}</span>
              </Alert>
            )}

            <div className="space-y-1.5">
              <Label htmlFor="settings-key">Project Key</Label>
              <Input
                id="settings-key"
                value={project.key}
                disabled
                className="font-mono bg-muted text-muted-foreground cursor-not-allowed"
              />
              <p className="text-[11px] text-muted-foreground">
                The project key is immutable and permanently uniquely identifies issues in this project.
              </p>
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="settings-name">Project Name</Label>
              <Input
                id="settings-name"
                disabled={!canEdit}
                error={!!errors.name}
                {...register('name')}
              />
              {errors.name && (
                <p className="text-xs text-destructive">{errors.name.message}</p>
              )}
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="settings-description">Description</Label>
              <textarea
                id="settings-description"
                rows={4}
                disabled={!canEdit}
                className="flex w-full rounded-md border border-input bg-transparent px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
                {...register('description')}
              />
              {errors.description && (
                <p className="text-xs text-destructive">{errors.description.message}</p>
              )}
            </div>
          </CardContent>

          {canEdit && (
            <CardFooter className="flex items-center justify-end border-t border-border/50 pt-4">
              <Button type="submit" isLoading={isSubmitting} disabled={!isDirty}>
                Save Changes
              </Button>
            </CardFooter>
          )}
        </form>
      </Card>

      {/* Danger Zone (Owner Only) */}
      <Card className="border-destructive/30 bg-destructive/5">
        <CardHeader>
          <CardTitle className="text-lg text-destructive flex items-center gap-2">
            <AlertTriangle className="h-5 w-5" />
            Danger Zone
          </CardTitle>
          <CardDescription>
            High-impact administrative actions requiring project ownership
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          {!project.isArchived ? (
            <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 p-4 rounded-lg border border-border bg-card">
              <div className="space-y-1">
                <h2 className="font-semibold text-sm text-foreground">Archive this project</h2>
                <p className="text-xs text-muted-foreground max-w-md">
                  Mark this project as read-only. Members will not be able to create or edit items, add members, or change settings until restored.
                </p>
              </div>
              {isOwner ? (
                <Button
                  variant="destructive"
                  size="sm"
                  onClick={() => setIsArchiveModalOpen(true)}
                  className="shrink-0"
                >
                  <Archive className="mr-1.5 h-4 w-4" />
                  Archive Project
                </Button>
              ) : (
                <p className="text-xs text-muted-foreground italic shrink-0">
                  Only the Owner can archive
                </p>
              )}
            </div>
          ) : (
            <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 p-4 rounded-lg border border-border bg-card">
              <div className="space-y-1">
                <h2 className="font-semibold text-sm text-foreground">Restore this project</h2>
                <p className="text-xs text-muted-foreground max-w-md">
                  Re-activate this project and allow members to resume collaborating and editing items.
                </p>
              </div>
              {isOwner ? (
                <Button
                  variant="primary"
                  size="sm"
                  onClick={() => setIsRestoreModalOpen(true)}
                  className="shrink-0"
                >
                  <RotateCcw className="mr-1.5 h-4 w-4" />
                  Restore Project
                </Button>
              ) : (
                <p className="text-xs text-muted-foreground italic shrink-0">
                  Only the Owner can restore
                </p>
              )}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Archive Modal */}
      <Modal
        isOpen={isArchiveModalOpen}
        onClose={() => {
          setIsArchiveModalOpen(false)
          setArchiveError(null)
        }}
        title="Archive Project"
        description={`Are you sure you want to archive "${project.name}"?`}
      >
        <div className="space-y-4">
          {archiveError && <Alert variant="destructive">{archiveError}</Alert>}

          <p className="text-xs text-muted-foreground leading-relaxed">
            Archiving disables all updates, task transitions, and member additions. The project will remain visible in read-only mode to existing members and can be restored at any time by the Owner.
          </p>

          <div className="flex items-center justify-end gap-2 pt-2 border-t border-border/50">
            <Button
              variant="outline"
              onClick={() => {
                setIsArchiveModalOpen(false)
                setArchiveError(null)
              }}
            >
              Cancel
            </Button>
            <Button
              variant="destructive"
              onClick={handleArchiveConfirm}
              isLoading={archiveMutation.isPending}
            >
              Archive Project
            </Button>
          </div>
        </div>
      </Modal>

      {/* Restore Modal */}
      <Modal
        isOpen={isRestoreModalOpen}
        onClose={() => {
          setIsRestoreModalOpen(false)
          setRestoreError(null)
        }}
        title="Restore Project"
        description={`Restore "${project.name}" back to active status?`}
      >
        <div className="space-y-4">
          {restoreError && <Alert variant="destructive">{restoreError}</Alert>}

          <p className="text-xs text-muted-foreground leading-relaxed">
            Restoring enables all team members to resume modifying project details, creating tasks, and managing memberships.
          </p>

          <div className="flex items-center justify-end gap-2 pt-2 border-t border-border/50">
            <Button
              variant="outline"
              onClick={() => {
                setIsRestoreModalOpen(false)
                setRestoreError(null)
              }}
            >
              Cancel
            </Button>
            <Button
              variant="primary"
              onClick={handleRestoreConfirm}
              isLoading={restoreMutation.isPending}
            >
              Restore Project
            </Button>
          </div>
        </div>
      </Modal>
    </div>
  )
}
