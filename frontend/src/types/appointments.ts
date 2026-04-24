export interface AppointmentSummary {
  appointmentId: string
  patientId: string
  patientName: string
  mrn: string
  doctorUserId: string
  doctorName: string
  scheduledAt: string
  durationMinutes: number
  status: string
  reason: string | null
  notes: string | null
  createdAt: string
}

export interface CreateAppointmentRequest {
  patientId: string
  doctorUserId: string
  scheduledAt: string
  durationMinutes: number
  reason?: string
  notes?: string
}

export interface UpdateAppointmentRequest {
  scheduledAt?: string
  durationMinutes?: number
  reason?: string
  notes?: string
}

export const APPOINTMENT_STATUS_COLORS: Record<string, string> = {
  Scheduled: 'bg-blue-100 text-blue-700',
  Confirmed: 'bg-green-100 text-green-700',
  CheckedIn: 'bg-yellow-100 text-yellow-700',
  Completed: 'bg-gray-100 text-gray-700',
  Cancelled: 'bg-red-100 text-red-700',
  NoShow: 'bg-orange-100 text-orange-700',
}
