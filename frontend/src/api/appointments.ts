import { apiClient } from './client'
import type { AppointmentSummary, CreateAppointmentRequest, UpdateAppointmentRequest } from '../types/appointments'

export const getAppointments = (params?: { from?: string; to?: string; doctorUserId?: string; status?: string }) =>
  apiClient.get<AppointmentSummary[]>('/appointments/calendar', { params }).then((r) => r.data)

export const getAppointmentsForPatient = (patientId: string) =>
  apiClient.get<AppointmentSummary[]>('/appointments', { params: { patientId } }).then((r) => r.data)

export const getAppointment = (id: string) =>
  apiClient.get<AppointmentSummary>(`/appointments/${id}`).then((r) => r.data)

export const createAppointment = (data: CreateAppointmentRequest) =>
  apiClient.post<AppointmentSummary>('/appointments', data).then((r) => r.data)

export const updateAppointment = (id: string, data: UpdateAppointmentRequest) =>
  apiClient.put<AppointmentSummary>(`/appointments/${id}`, data).then((r) => r.data)

export const confirmAppointment = (id: string) =>
  apiClient.post(`/appointments/${id}/confirm`).then((r) => r.data)

export const cancelAppointment = (id: string) =>
  apiClient.post(`/appointments/${id}/cancel`).then((r) => r.data)

export const checkInAppointment = (id: string) =>
  apiClient.post(`/appointments/${id}/checkin`).then((r) => r.data)
