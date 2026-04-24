import { apiClient } from './client'
import type { LoginRequest, LoginResponse, ChangePasswordRequest } from '../types/auth'

export const login = (data: LoginRequest) =>
  apiClient.post<LoginResponse>('/auth/login', {
    email: data.email,
    password: data.password,
  }, {
    headers: { 'X-Tenant-Code': data.tenantCode },
  }).then((r) => r.data)

export const changePassword = (data: ChangePasswordRequest) =>
  apiClient.post('/auth/change-password', data)
