import { ApiError } from '@/features/auth/api/authApi'
import {
  AddMemberPayload,
  CreateProjectPayload,
  PagedProjectsDto,
  ProjectDto,
  ProjectMemberDto,
  UpdateMemberRolePayload,
  UpdateProjectPayload,
} from '../types'

const BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000'

async function authFetch<T>(
  url: string,
  token: string,
  options: RequestInit = {}
): Promise<T> {
  const headers = new Headers(options.headers || {})
  headers.set('Authorization', `Bearer ${token}`)
  if (!headers.has('Content-Type') && options.body) {
    headers.set('Content-Type', 'application/json')
  }

  const res = await fetch(url, {
    ...options,
    headers,
  })

  if (res.status === 204) {
    return undefined as unknown as T
  }

  if (res.ok) {
    return res.json() as Promise<T>
  }

  let message = `Request failed with status ${res.status}`
  let errorData: any

  try {
    errorData = await res.json()
    if (errorData?.detail) {
      message = errorData.detail
    } else if (errorData?.errors) {
      const firstField = Object.keys(errorData.errors)[0]
      if (firstField && errorData.errors[firstField]?.length) {
        message = errorData.errors[firstField][0]
      }
    } else if (errorData?.title) {
      message = errorData.title
    }
  } catch {
    const text = await res.text().catch(() => '')
    if (text) message = text
  }

  throw new ApiError(message, res.status, errorData)
}

export const projectsApi = {
  async listProjects(
    token: string,
    params: { page?: number; pageSize?: number; search?: string; includeArchived?: boolean } = {}
  ): Promise<PagedProjectsDto> {
    const query = new URLSearchParams()
    if (params.page) query.set('page', params.page.toString())
    if (params.pageSize) query.set('pageSize', params.pageSize.toString())
    if (params.search) query.set('search', params.search)
    if (params.includeArchived !== undefined) query.set('includeArchived', params.includeArchived.toString())

    const url = `${BASE_URL}/api/v1/projects${query.toString() ? `?${query.toString()}` : ''}`
    return authFetch<PagedProjectsDto>(url, token)
  },

  async getProject(token: string, projectId: string): Promise<ProjectDto> {
    return authFetch<ProjectDto>(`${BASE_URL}/api/v1/projects/${projectId}`, token)
  },

  async createProject(token: string, payload: CreateProjectPayload): Promise<ProjectDto> {
    return authFetch<ProjectDto>(`${BASE_URL}/api/v1/projects`, token, {
      method: 'POST',
      body: JSON.stringify(payload),
    })
  },

  async updateProject(token: string, projectId: string, payload: UpdateProjectPayload): Promise<ProjectDto> {
    return authFetch<ProjectDto>(`${BASE_URL}/api/v1/projects/${projectId}`, token, {
      method: 'PATCH',
      body: JSON.stringify(payload),
    })
  },

  async archiveProject(token: string, projectId: string): Promise<ProjectDto> {
    return authFetch<ProjectDto>(`${BASE_URL}/api/v1/projects/${projectId}/archive`, token, {
      method: 'POST',
    })
  },

  async restoreProject(token: string, projectId: string): Promise<ProjectDto> {
    return authFetch<ProjectDto>(`${BASE_URL}/api/v1/projects/${projectId}/restore`, token, {
      method: 'POST',
    })
  },

  async listMembers(token: string, projectId: string): Promise<ProjectMemberDto[]> {
    return authFetch<ProjectMemberDto[]>(`${BASE_URL}/api/v1/projects/${projectId}/members`, token)
  },

  async addMember(token: string, projectId: string, payload: AddMemberPayload): Promise<ProjectMemberDto> {
    return authFetch<ProjectMemberDto>(`${BASE_URL}/api/v1/projects/${projectId}/members`, token, {
      method: 'POST',
      body: JSON.stringify(payload),
    })
  },

  async updateMemberRole(
    token: string,
    projectId: string,
    userId: string,
    payload: UpdateMemberRolePayload
  ): Promise<ProjectMemberDto> {
    return authFetch<ProjectMemberDto>(`${BASE_URL}/api/v1/projects/${projectId}/members/${userId}`, token, {
      method: 'PATCH',
      body: JSON.stringify(payload),
    })
  },

  async removeMember(token: string, projectId: string, userId: string): Promise<void> {
    return authFetch<void>(`${BASE_URL}/api/v1/projects/${projectId}/members/${userId}`, token, {
      method: 'DELETE',
    })
  },

  async leaveProject(token: string, projectId: string): Promise<void> {
    return authFetch<void>(`${BASE_URL}/api/v1/projects/${projectId}/leave`, token, {
      method: 'POST',
    })
  },
}
