import React, { useState } from 'react'
import { useParams, Link, useNavigate } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { 
  useProject, 
  useProjectMembers, 
  useAddMember, 
  useUpdateMemberRole, 
  useRemoveMember, 
  useLeaveProject 
} from '../hooks/useProjects'
import { addMemberSchema, AddMemberFormData } from '../schemas/projectSchemas'
import { Button } from '@/shared/components/ui/Button'
import { Input } from '@/shared/components/ui/Input'
import { Label } from '@/shared/components/ui/Label'
import { Alert } from '@/shared/components/ui/Alert'
import { Modal } from '@/shared/components/ui/Modal'
import { Skeleton } from '@/shared/components/ui/Skeleton'
import { 
  UserPlus, 
  Users, 
  LogOut, 
  Trash2, 
  Crown, 
  ShieldCheck, 
  UserCheck, 
  AlertTriangle 
} from 'lucide-react'

export const ProjectMembersPage: React.FC = () => {
  const { projectId } = useParams<{ projectId: string }>()
  const navigate = useNavigate()

  const { data: project, isLoading: isProjectLoading } = useProject(projectId)
  const { data: members, isLoading: isMembersLoading, error } = useProjectMembers(projectId)

  const addMemberMutation = useAddMember(projectId!)
  const updateRoleMutation = useUpdateMemberRole(projectId!)
  const removeMemberMutation = useRemoveMember(projectId!)
  const leaveProjectMutation = useLeaveProject(projectId!)

  const [isAddOpen, setIsAddOpen] = useState(false)
  const [addError, setAddError] = useState<string | null>(null)

  const [memberToRemove, setMemberToRemove] = useState<{ id: string; name: string } | null>(null)
  const [removeError, setRemoveError] = useState<string | null>(null)

  const [isLeaveOpen, setIsLeaveOpen] = useState(false)
  const [leaveError, setLeaveError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors: formErrors, isSubmitting: isAdding },
  } = useForm<AddMemberFormData>({
    resolver: zodResolver(addMemberSchema),
    defaultValues: {
      email: '',
      role: 'Member',
    },
  })

  if (isProjectLoading || isMembersLoading) {
    return (
      <div className="max-w-5xl mx-auto space-y-6">
        <Skeleton className="h-6 w-32" />
        <Skeleton className="h-10 w-64" />
        <Skeleton className="h-64 w-full" />
      </div>
    )
  }

  if (!project) {
    return null
  }

  const isOwner = project.currentUserRole === 'Owner'
  const isAdmin = project.currentUserRole === 'Admin'
  const canManageMembers = (isOwner || isAdmin) && !project.isArchived

  const getInitials = (name?: string) => {
    if (!name) return 'U'
    const parts = name.trim().split(' ')
    if (parts.length >= 2) return `${parts[0][0]}${parts[1][0]}`.toUpperCase()
    return name.slice(0, 2).toUpperCase()
  }

  const onAddSubmit = async (data: AddMemberFormData) => {
    try {
      setAddError(null)
      await addMemberMutation.mutateAsync({
        email: data.email,
        role: data.role,
      })
      reset()
      setIsAddOpen(false)
    } catch (err: any) {
      setAddError(err.message || 'Failed to add member.')
    }
  }

  const handleRoleChange = async (userId: string, newRole: 'Admin' | 'Member') => {
    try {
      await updateRoleMutation.mutateAsync({
        userId,
        payload: { role: newRole },
      })
    } catch (err: any) {
      alert(err.message || 'Failed to update member role.')
    }
  }

  const handleRemoveConfirm = async () => {
    if (!memberToRemove) return
    try {
      setRemoveError(null)
      await removeMemberMutation.mutateAsync(memberToRemove.id)
      setMemberToRemove(null)
    } catch (err: any) {
      setRemoveError(err.message || 'Failed to remove member.')
    }
  }

  const handleLeaveConfirm = async () => {
    try {
      setLeaveError(null)
      await leaveProjectMutation.mutateAsync()
      navigate('/projects', { replace: true })
    } catch (err: any) {
      setLeaveError(err.message || 'Failed to leave project.')
    }
  }

  return (
    <div className="max-w-5xl mx-auto space-y-6 animate-in fade-in duration-200">
      {/* Breadcrumb Navigation */}
      <div className="flex items-center gap-2 text-xs text-muted-foreground">
        <Link to="/projects" className="hover:text-foreground transition-colors">
          Projects
        </Link>
        <span>/</span>
        <Link to={`/projects/${project.id}`} className="hover:text-foreground transition-colors font-mono">
          {project.key}
        </Link>
        <span>/</span>
        <span className="text-foreground">Members</span>
      </div>

      {/* Header Bar */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-4 border-b border-border">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-foreground flex items-center gap-2.5">
            <Users className="h-6 w-6 text-primary" />
            Project Members
          </h1>
          <p className="text-xs text-muted-foreground mt-0.5">
            Collaborators with access to <span className="font-semibold text-foreground">{project.name}</span>
          </p>
        </div>

        <div className="flex items-center gap-2">
          {canManageMembers && (
            <Button onClick={() => setIsAddOpen(true)} size="sm">
              <UserPlus className="mr-1.5 h-4 w-4" />
              Add Member
            </Button>
          )}

          {!isOwner && !project.isArchived && (
            <Button
              onClick={() => setIsLeaveOpen(true)}
              variant="outline"
              size="sm"
              className="text-destructive hover:bg-destructive/10 hover:text-destructive"
            >
              <LogOut className="mr-1.5 h-4 w-4" />
              Leave Project
            </Button>
          )}
        </div>
      </div>

      {error && (
        <Alert variant="destructive">
          {(error as any)?.message || 'Failed to load project members.'}
        </Alert>
      )}

      {/* Members Table Card */}
      <div className="rounded-xl border border-border bg-card overflow-hidden shadow-xs">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-sm">
            <thead className="bg-muted/50 border-b border-border text-xs text-muted-foreground uppercase tracking-wider">
              <tr>
                <th className="py-3 px-4 font-semibold">User</th>
                <th className="py-3 px-4 font-semibold">Role</th>
                <th className="py-3 px-4 font-semibold hidden sm:table-cell">Joined</th>
                <th className="py-3 px-4 font-semibold text-right">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border/60">
              {members?.map(member => {
                const isTargetOwner = member.role === 'Owner'
                const isTargetAdmin = member.role === 'Admin'
                const canModifyThisMember =
                  !project.isArchived &&
                  !isTargetOwner &&
                  (isOwner || (isAdmin && !isTargetAdmin))

                return (
                  <tr key={member.userId} className="hover:bg-muted/30 transition-colors">
                    <td className="py-3.5 px-4">
                      <div className="flex items-center gap-3">
                        <div className="h-9 w-9 rounded-full bg-primary/10 text-primary font-bold text-xs flex items-center justify-center border border-primary/20 shrink-0">
                          {getInitials(member.displayName)}
                        </div>
                        <div className="min-w-0">
                          <p className="font-medium text-foreground text-sm truncate">{member.displayName}</p>
                          <p className="text-xs text-muted-foreground truncate">{member.email}</p>
                        </div>
                      </div>
                    </td>

                    <td className="py-3.5 px-4">
                      {canModifyThisMember && isOwner ? (
                        <select
                          value={member.role}
                          onChange={e => handleRoleChange(member.userId, e.target.value as any)}
                          className="text-xs rounded-md border border-input bg-card px-2 py-1 text-foreground focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring cursor-pointer"
                        >
                          <option value="Admin">Admin</option>
                          <option value="Member">Member</option>
                        </select>
                      ) : (
                        <div className="inline-flex items-center">
                          {member.role === 'Owner' && (
                            <span className="inline-flex items-center gap-1 text-xs font-semibold px-2.5 py-0.5 rounded-full bg-amber-500/10 text-amber-600 dark:text-amber-400 border border-amber-500/20">
                              <Crown className="h-3 w-3" />
                              Owner
                            </span>
                          )}
                          {member.role === 'Admin' && (
                            <span className="inline-flex items-center gap-1 text-xs font-semibold px-2.5 py-0.5 rounded-full bg-purple-500/10 text-purple-600 dark:text-purple-400 border border-purple-500/20">
                              <ShieldCheck className="h-3 w-3" />
                              Admin
                            </span>
                          )}
                          {member.role === 'Member' && (
                            <span className="inline-flex items-center gap-1 text-xs font-medium px-2.5 py-0.5 rounded-full bg-muted text-muted-foreground border border-border">
                              <UserCheck className="h-3 w-3" />
                              Member
                            </span>
                          )}
                        </div>
                      )}
                    </td>

                    <td className="py-3.5 px-4 text-xs text-muted-foreground hidden sm:table-cell">
                      {new Date(member.joinedAtUtc).toLocaleDateString()}
                    </td>

                    <td className="py-3.5 px-4 text-right">
                      {canModifyThisMember ? (
                        <button
                          type="button"
                          onClick={() => setMemberToRemove({ id: member.userId, name: member.displayName })}
                          className="p-1.5 rounded-md text-muted-foreground hover:text-destructive hover:bg-destructive/10 transition-colors cursor-pointer"
                          title="Remove member"
                        >
                          <Trash2 className="h-4 w-4" />
                        </button>
                      ) : (
                        <span className="text-xs text-muted-foreground/60 italic">
                          {isTargetOwner ? 'Owner' : '—'}
                        </span>
                      )}
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      </div>

      {/* Add Member Modal */}
      <Modal
        isOpen={isAddOpen}
        onClose={() => {
          setIsAddOpen(false)
          reset()
          setAddError(null)
        }}
        title="Add project member"
        description="Invite an existing registered user to collaborate on this project"
      >
        <form onSubmit={handleSubmit(onAddSubmit)} className="space-y-4">
          {addError && <Alert variant="destructive">{addError}</Alert>}

          <div className="space-y-1.5">
            <Label htmlFor="member-email">User Email Address</Label>
            <Input
              id="member-email"
              type="email"
              placeholder="colleague@example.com"
              autoFocus
              error={!!formErrors.email}
              {...register('email')}
            />
            {formErrors.email && (
              <p className="text-xs text-destructive">{formErrors.email.message}</p>
            )}
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="member-role">Assigned Role</Label>
            <select
              id="member-role"
              className="flex h-10 w-full rounded-md border border-input bg-transparent px-3 py-2 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
              {...register('role')}
            >
              <option value="Member">Member (Read & Contribute)</option>
              {isOwner && <option value="Admin">Admin (Project Management)</option>}
            </select>
            {isAdmin && !isOwner && (
              <p className="text-[11px] text-muted-foreground">
                As an Admin, you can add new Members. Only the Owner can assign Admin roles.
              </p>
            )}
          </div>

          <div className="flex items-center justify-end gap-2 pt-2 border-t border-border/50">
            <Button
              type="button"
              variant="outline"
              onClick={() => {
                setIsAddOpen(false)
                reset()
                setAddError(null)
              }}
            >
              Cancel
            </Button>
            <Button type="submit" isLoading={isAdding}>
              Add Member
            </Button>
          </div>
        </form>
      </Modal>

      {/* Remove Member Confirmation Modal */}
      <Modal
        isOpen={!!memberToRemove}
        onClose={() => {
          setMemberToRemove(null)
          setRemoveError(null)
        }}
        title="Remove member"
        description="Are you sure you want to remove this member from the project?"
      >
        <div className="space-y-4">
          {removeError && <Alert variant="destructive">{removeError}</Alert>}

          <p className="text-sm text-foreground">
            <span className="font-semibold">{memberToRemove?.name}</span> will lose all access to this workspace and its associated items.
          </p>

          <div className="flex items-center justify-end gap-2 pt-2 border-t border-border/50">
            <Button
              variant="outline"
              onClick={() => {
                setMemberToRemove(null)
                setRemoveError(null)
              }}
            >
              Cancel
            </Button>
            <Button
              variant="destructive"
              onClick={handleRemoveConfirm}
              isLoading={removeMemberMutation.isPending}
            >
              Remove Member
            </Button>
          </div>
        </div>
      </Modal>

      {/* Leave Project Confirmation Modal */}
      <Modal
        isOpen={isLeaveOpen}
        onClose={() => {
          setIsLeaveOpen(false)
          setLeaveError(null)
        }}
        title="Leave Project"
        description="Confirm leaving this project workspace"
      >
        <div className="space-y-4">
          {leaveError && <Alert variant="destructive">{leaveError}</Alert>}

          <div className="flex items-start gap-3 p-3 rounded-lg bg-destructive/10 text-destructive border border-destructive/20 text-xs">
            <AlertTriangle className="h-5 w-5 shrink-0 mt-0.5" />
            <span>
              You will immediately lose access to this project. You will need to be re-invited by the project owner or an admin to rejoin.
            </span>
          </div>

          <div className="flex items-center justify-end gap-2 pt-2 border-t border-border/50">
            <Button
              variant="outline"
              onClick={() => {
                setIsLeaveOpen(false)
                setLeaveError(null)
              }}
            >
              Cancel
            </Button>
            <Button
              variant="destructive"
              onClick={handleLeaveConfirm}
              isLoading={leaveProjectMutation.isPending}
            >
              Leave Project
            </Button>
          </div>
        </div>
      </Modal>
    </div>
  )
}
