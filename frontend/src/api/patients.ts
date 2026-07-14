import { apiClient } from './client'
import type { RegisterPatientRequest, UpdatePatientRequest } from '../types/patients'

function mapBackendPatient(p: any): any {
  if (!p) return p
  let firstName = p.firstName
  let lastName = p.lastName
  if (!firstName && p.fullName) {
    const parts = p.fullName.split(' ')
    firstName = parts[0]
    lastName = parts.slice(1).join(' ')
  }
  return {
    ...p,
    mrn: p.mrn || p.medicalRecordNumber,
    firstName: firstName || '',
    lastName: lastName || '',
    phone: p.phone || p.phoneNumber,
    email: p.email,
    address: p.address || p.addressLine1,
    bloodGroup: p.bloodGroup || p.bloodType,
    nhisNumber: p.nhisNumber || p.nationalId,
    createdAt: p.createdAt || p.registeredAt,
  }
}

export const searchPatients = (query?: string, page = 1, pageSize = 20) =>
  apiClient.get<{ total: number; page: number; pageSize: number; items: any[] }>('/patients', {
    params: { query, page, pageSize },
  }).then((r) => ({
    ...r.data,
    items: r.data.items.map(mapBackendPatient),
  }))

export const getPatient = (id: string) =>
  apiClient.get<any>(`/patients/${id}`).then((r) => mapBackendPatient(r.data))

export const registerPatient = (data: RegisterPatientRequest) => {
  const backendData = {
    firstName: data.firstName,
    lastName: data.lastName,
    dateOfBirth: data.dateOfBirth,
    gender: data.gender,
    phoneNumber: data.phone,
    email: data.email,
    addressLine1: data.address,
    bloodType: data.bloodGroup,
    nationalId: data.nhisNumber,
    emergencyContactName: data.emergencyContactName,
    emergencyContactPhone: data.emergencyContactPhone,
    notes: data.notes,
  }
  return apiClient.post<any>('/patients', backendData).then((r) => mapBackendPatient(r.data))
}

export const updatePatient = (id: string, data: UpdatePatientRequest) => {
  const backendData = {
    firstName: data.firstName,
    lastName: data.lastName,
    phoneNumber: data.phone,
    email: data.email,
    addressLine1: data.address,
    bloodType: data.bloodGroup,
    nationalId: data.nhisNumber,
    emergencyContactName: data.emergencyContactName,
    emergencyContactPhone: data.emergencyContactPhone,
    notes: data.notes,
  }
  return apiClient.put<any>(`/patients/${id}`, backendData).then((r) => mapBackendPatient(r.data))
}

