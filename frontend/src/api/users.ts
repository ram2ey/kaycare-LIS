import { apiClient } from './client'
import type { UserResponse, CreateUserRequest, UpdateUserRequest, ResetPasswordRequest } from '../types/users'

export const getUsers = (params?: { roleId?: number; includeInactive?: boolean }) =>
  apiClient.get<UserResponse[]>('/users', { params }).then((r) => r.data)

export const getUser = (id: string) =>
  apiClient.get<UserResponse>(`/users/${id}`).then((r) => r.data)

export const createUser = (data: CreateUserRequest) =>
  apiClient.post<UserResponse>('/users', data).then((r) => r.data)

export const updateUser = (id: string, data: UpdateUserRequest) =>
  apiClient.put<UserResponse>(`/users/${id}`, data).then((r) => r.data)

export const deactivateUser = (id: string) =>
  apiClient.delete(`/users/${id}`).then((r) => r.data)

export const reactivateUser = (id: string) =>
  apiClient.post(`/users/${id}/reactivate`).then((r) => r.data)

export const resetPassword = (id: string, data: ResetPasswordRequest) =>
  apiClient.post(`/users/${id}/reset-password`, data).then((r) => r.data)

export const getDepartments = () =>
  apiClient.get<{ name: string; userCount: number }[]>('/users/departments').then((r) => r.data)

export const renameDepartment = (oldName: string, newName: string) =>
  apiClient.put('/users/departments/rename', { oldName, newName }).then((r) => r.data)
