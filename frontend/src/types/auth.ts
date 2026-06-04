export interface LoginRequest {
  email: string
  password: string
  tenantCode: string
}

export interface LoginResponse {
  token: string
  userId: string
  email: string
  firstName: string
  lastName: string
  role: string
  department: string | null
  mustChangePassword: boolean
}

export interface ChangePasswordRequest {
  currentPassword: string
  newPassword: string
}

export interface AuthUser {
  token: string
  userId: string
  email: string
  firstName: string
  lastName: string
  role: string
  department: string | null
  mustChangePassword: boolean
  tenantCode: string
}
