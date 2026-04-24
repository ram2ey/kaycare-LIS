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
  mustChangePassword: boolean
  tenantCode: string
}
