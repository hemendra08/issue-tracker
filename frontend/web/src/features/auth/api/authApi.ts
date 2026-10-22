import { LoginRequestDto, LoginResponseDto, ProblemDetailsDto, RegisterRequestDto, UserDto } from '../types'

const BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000'

export class ApiError extends Error {
  status: number
  data?: ProblemDetailsDto

  constructor(message: string, status: number, data?: ProblemDetailsDto) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.data = data
  }
}

async function handleResponse<T>(response: Response): Promise<T> {
  if (response.ok) {
    return response.json() as Promise<T>
  }

  let errorData: ProblemDetailsDto | undefined
  let message = `Request failed with status ${response.status}`

  try {
    errorData = await response.json()
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
    // Non-JSON error body fallback
    const text = await response.text().catch(() => '')
    if (text) message = text
  }

  throw new ApiError(message, response.status, errorData)
}

export const authApi = {
  async register(payload: RegisterRequestDto): Promise<UserDto> {
    const res = await fetch(`${BASE_URL}/api/v1/auth/register`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(payload),
    })
    return handleResponse<UserDto>(res)
  },

  async login(payload: LoginRequestDto): Promise<LoginResponseDto> {
    const res = await fetch(`${BASE_URL}/api/v1/auth/login`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(payload),
    })
    return handleResponse<LoginResponseDto>(res)
  },

  async getMe(token: string): Promise<UserDto> {
    const res = await fetch(`${BASE_URL}/api/v1/auth/me`, {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    })
    return handleResponse<UserDto>(res)
  },

  async logout(token: string): Promise<void> {
    await fetch(`${BASE_URL}/api/v1/auth/logout`, {
      method: 'POST',
      headers: {
        Authorization: `Bearer ${token}`,
      },
    })
  },
}
