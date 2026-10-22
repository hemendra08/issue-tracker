export interface UserDto {
  id: string
  email: string
  displayName: string
  createdAtUtc: string
}

export interface LoginResponseDto {
  accessToken: string
  tokenType: string
  expiresIn: number
}

export interface RegisterRequestDto {
  email: string
  displayName: string
  password: string
}

export interface LoginRequestDto {
  email: string
  password: string
}

export interface ProblemDetailsDto {
  type?: string
  title?: string
  status?: number
  detail?: string
  errors?: Record<string, string[]>
}
