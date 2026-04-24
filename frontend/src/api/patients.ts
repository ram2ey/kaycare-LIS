import { apiClient } from './client'
import type { PatientSummary, PatientDetail, RegisterPatientRequest, UpdatePatientRequest } from '../types/patients'

export const searchPatients = (query?: string, page = 1, pageSize = 20) =>
  apiClient.get<{ total: number; page: number; pageSize: number; items: PatientSummary[] }>('/patients', {
    params: { query, page, pageSize },
  }).then((r) => r.data)

export const getPatient = (id: string) =>
  apiClient.get<PatientDetail>(`/patients/${id}`).then((r) => r.data)

export const registerPatient = (data: RegisterPatientRequest) =>
  apiClient.post<PatientDetail>('/patients', data).then((r) => r.data)

export const updatePatient = (id: string, data: UpdatePatientRequest) =>
  apiClient.put<PatientDetail>(`/patients/${id}`, data).then((r) => r.data)
