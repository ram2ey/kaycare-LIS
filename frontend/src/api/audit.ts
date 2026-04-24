import { apiClient } from './client'
import type { AuditLogResponse } from '../types/audit'

export const getAuditLogs = (params?: {
  patientId?: string
  userId?: string
  action?: string
  from?: string
  to?: string
  page?: number
  pageSize?: number
}) =>
  apiClient.get<{ total: number; page: number; pageSize: number; items: AuditLogResponse[] }>(
    '/audit-logs',
    { params },
  ).then((r) => r.data)
