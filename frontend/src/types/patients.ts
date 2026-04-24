export interface PatientSummary {
  patientId: string
  mrn: string
  firstName: string
  lastName: string
  dateOfBirth: string
  gender: string
  phone: string
  email: string | null
  address: string | null
  isActive: boolean
  createdAt: string
}

export interface PatientDetail extends PatientSummary {
  bloodGroup: string | null
  nhisNumber: string | null
  emergencyContactName: string | null
  emergencyContactPhone: string | null
  notes: string | null
}

export interface RegisterPatientRequest {
  firstName: string
  lastName: string
  dateOfBirth: string
  gender: string
  phone: string
  email?: string
  address?: string
  bloodGroup?: string
  nhisNumber?: string
  emergencyContactName?: string
  emergencyContactPhone?: string
  notes?: string
}

export interface UpdatePatientRequest {
  firstName?: string
  lastName?: string
  phone?: string
  email?: string
  address?: string
  bloodGroup?: string
  nhisNumber?: string
  emergencyContactName?: string
  emergencyContactPhone?: string
  notes?: string
}

export interface PatientSearchRequest {
  query?: string
  page?: number
  pageSize?: number
}

export const GENDER_OPTIONS = ['Male', 'Female', 'Other'] as const
export const BLOOD_GROUP_OPTIONS = ['A+', 'A-', 'B+', 'B-', 'AB+', 'AB-', 'O+', 'O-'] as const
