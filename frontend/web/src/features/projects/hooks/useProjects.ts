import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useAuth } from '@/features/auth/hooks/useAuth'
import { projectsApi } from '../api/projectsApi'
import {
  AddMemberPayload,
  CreateProjectPayload,
  UpdateMemberRolePayload,
  UpdateProjectPayload,
} from '../types'

export function useProjects(params: {
  page?: number
  pageSize?: number
  search?: string
  includeArchived?: boolean
} = {}) {
  const { token } = useAuth()
  return useQuery({
    queryKey: ['projects', params],
    queryFn: () => (token ? projectsApi.listProjects(token, params) : Promise.reject('No token')),
    enabled: !!token,
  })
}

export function useProject(projectId?: string) {
  const { token } = useAuth()
  return useQuery({
    queryKey: ['projects', projectId],
    queryFn: () => (token && projectId ? projectsApi.getProject(token, projectId) : Promise.reject('No token')),
    enabled: !!token && !!projectId,
  })
}

export function useProjectMembers(projectId?: string) {
  const { token } = useAuth()
  return useQuery({
    queryKey: ['projects', projectId, 'members'],
    queryFn: () => (token && projectId ? projectsApi.listMembers(token, projectId) : Promise.reject('No token')),
    enabled: !!token && !!projectId,
  })
}

export function useCreateProject() {
  const { token } = useAuth()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (payload: CreateProjectPayload) => {
      if (!token) throw new Error('Not authenticated')
      return projectsApi.createProject(token, payload)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['projects'] })
    },
  })
}

export function useUpdateProject(projectId: string) {
  const { token } = useAuth()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (payload: UpdateProjectPayload) => {
      if (!token) throw new Error('Not authenticated')
      return projectsApi.updateProject(token, projectId, payload)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['projects'] })
      queryClient.invalidateQueries({ queryKey: ['projects', projectId] })
    },
  })
}

export function useArchiveProject(projectId: string) {
  const { token } = useAuth()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: () => {
      if (!token) throw new Error('Not authenticated')
      return projectsApi.archiveProject(token, projectId)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['projects'] })
      queryClient.invalidateQueries({ queryKey: ['projects', projectId] })
    },
  })
}

export function useRestoreProject(projectId: string) {
  const { token } = useAuth()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: () => {
      if (!token) throw new Error('Not authenticated')
      return projectsApi.restoreProject(token, projectId)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['projects'] })
      queryClient.invalidateQueries({ queryKey: ['projects', projectId] })
    },
  })
}

export function useAddMember(projectId: string) {
  const { token } = useAuth()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (payload: AddMemberPayload) => {
      if (!token) throw new Error('Not authenticated')
      return projectsApi.addMember(token, projectId, payload)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['projects', projectId, 'members'] })
      queryClient.invalidateQueries({ queryKey: ['projects', projectId] })
      queryClient.invalidateQueries({ queryKey: ['projects'] })
    },
  })
}

export function useUpdateMemberRole(projectId: string) {
  const { token } = useAuth()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ userId, payload }: { userId: string; payload: UpdateMemberRolePayload }) => {
      if (!token) throw new Error('Not authenticated')
      return projectsApi.updateMemberRole(token, projectId, userId, payload)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['projects', projectId, 'members'] })
    },
  })
}

export function useRemoveMember(projectId: string) {
  const { token } = useAuth()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (userId: string) => {
      if (!token) throw new Error('Not authenticated')
      return projectsApi.removeMember(token, projectId, userId)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['projects', projectId, 'members'] })
      queryClient.invalidateQueries({ queryKey: ['projects', projectId] })
      queryClient.invalidateQueries({ queryKey: ['projects'] })
    },
  })
}

export function useLeaveProject(projectId: string) {
  const { token } = useAuth()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: () => {
      if (!token) throw new Error('Not authenticated')
      return projectsApi.leaveProject(token, projectId)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['projects'] })
      queryClient.removeQueries({ queryKey: ['projects', projectId] })
    },
  })
}
