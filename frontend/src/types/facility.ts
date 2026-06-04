export interface FacilitySettingsResponse {
  facilitySettingsId: string | null
  facilityName: string
  address: string | null
  phone: string | null
  email: string | null
  logoUrl: string | null
  isLaboratoryEnabled: boolean
  isRadiologyEnabled: boolean
}

export interface SaveFacilitySettingsRequest {
  facilityName: string
  address?: string
  phone?: string
  email?: string
  isLaboratoryEnabled?: boolean
  isRadiologyEnabled?: boolean
}
