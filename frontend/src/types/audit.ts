export interface AuditLogResponse {
  auditLogId: number
  tenantId: string
  userId: string | null
  userEmail: string | null
  action: string
  entityType: string | null
  entityId: string | null
  patientId: string | null
  details: string | null
  ipAddress: string | null
  timestamp: string
}
