export interface TenantResponse {
  tenantId: string
  tenantCode: string
  tenantName: string
  subdomain: string
  subscriptionPlan: string
  isActive: boolean
  maxUsers: number
  storageQuotaGB: number
  userCount: number
  isLaboratoryEnabled: boolean
  isRadiologyEnabled: boolean
  createdAt: string
}

export interface CreateTenantRequest {
  tenantCode: string
  tenantName: string
  subscriptionPlan: string
  maxUsers: number
  storageQuotaGB: number
  adminEmail: string
  adminFirstName: string
  adminLastName: string
  isLaboratoryEnabled?: boolean
  isRadiologyEnabled?: boolean
}

export interface UpdateTenantRequest {
  tenantName: string
  subscriptionPlan: string
  maxUsers: number
  storageQuotaGB: number
  isLaboratoryEnabled?: boolean
  isRadiologyEnabled?: boolean
}
