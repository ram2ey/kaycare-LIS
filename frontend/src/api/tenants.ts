import { apiClient } from './client'
import type { TenantResponse, CreateTenantRequest, UpdateTenantRequest } from '../types/tenants'

export const getTenants = () =>
  apiClient.get<TenantResponse[]>('/tenants').then((r) => r.data)

export const getTenant = (id: string) =>
  apiClient.get<TenantResponse>(`/tenants/${id}`).then((r) => r.data)

export const createTenant = (data: CreateTenantRequest) =>
  apiClient.post<TenantResponse>('/tenants', data).then((r) => r.data)

export const updateTenant = (id: string, data: UpdateTenantRequest) =>
  apiClient.put<TenantResponse>(`/tenants/${id}`, data).then((r) => r.data)

export const setTenantActive = (id: string, active: boolean) =>
  apiClient.patch<TenantResponse>(`/tenants/${id}/active`, null, {
    params: { value: active },
  }).then((r) => r.data)

export const deleteTenant = (id: string) =>
  apiClient.delete(`/tenants/${id}`).then((r) => r.data)
