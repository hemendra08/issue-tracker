export type ProjectRoleType = 'Owner' | 'Admin' | 'Member'

export interface ProjectDto {
  id: string
  name: string
  key: string
  description: string | null
  ownerId: string
  ownerName: string | null
  ownerEmail: string | null
  currentUserRole: ProjectRoleType
  memberCount: number
  isArchived: boolean
  createdAtUtc: string
  updatedAtUtc: string
}

export interface PagedProjectsDto {
  items: ProjectDto[]
  totalCount: number
  page: number
  pageSize: number
}

export interface ProjectMemberDto {
  projectId: string
  userId: string
  email: string
  displayName: string
  role: ProjectRoleType
  joinedAtUtc: string
}

export interface CreateProjectPayload {
  name: string
  key: string
  description?: string
}

export interface UpdateProjectPayload {
  name?: string
  description?: string
}

export interface AddMemberPayload {
  email: string
  role: 'Admin' | 'Member'
}

export interface UpdateMemberRolePayload {
  role: 'Admin' | 'Member'
}
