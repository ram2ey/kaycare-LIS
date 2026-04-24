import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { getAppointments, confirmAppointment, cancelAppointment, checkInAppointment, createAppointment } from '../api/appointments'
import { searchPatients } from '../api/patients'
import { getUsers } from '../api/users'
import type { AppointmentSummary } from '../types/appointments'
import type { CreateAppointmentRequest } from '../types/appointments'
import type { UserResponse } from '../types/users'
import { APPOINTMENT_STATUS_COLORS } from '../types/appointments'

export function AppointmentsPage() {
  const [appointments, setAppointments] = useState<AppointmentSummary[]>([])
  const [dateFrom, setDateFrom] = useState(() => new Date().toISOString().slice(0, 10))
  const [loading, setLoading] = useState(true)
  const [showCreate, setShowCreate] = useState(false)
  const [doctors, setDoctors] = useState<UserResponse[]>([])

  function load() {
    setLoading(true)
    const from = dateFrom ? `${dateFrom}T00:00:00` : undefined
    const to = dateFrom ? `${dateFrom}T23:59:59` : undefined
    getAppointments({ from, to })
      .then(setAppointments)
      .finally(() => setLoading(false))
  }

  useEffect(() => { load() }, [dateFrom])
  useEffect(() => { getUsers({ roleId: 3 }).then(setDoctors) }, [])

  async function handleAction(id: string, action: 'confirm' | 'cancel' | 'checkin') {
    if (action === 'cancel' && !confirm('Cancel this appointment?')) return
    if (action === 'confirm') await confirmAppointment(id)
    else if (action === 'cancel') await cancelAppointment(id)
    else await checkInAppointment(id)
    load()
  }

  return (
    <div className="p-6">
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Appointments</h1>
        <button
          onClick={() => setShowCreate(true)}
          className="bg-sky-700 hover:bg-sky-800 text-white px-4 py-2 rounded-lg text-sm font-medium"
        >
          + Book Appointment
        </button>
      </div>

      <div className="flex gap-3 mb-4 items-center">
        <input
          type="date"
          value={dateFrom}
          onChange={(e) => setDateFrom(e.target.value)}
          className="border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
        />
        <p className="text-sm text-gray-500">{appointments.length} appointments</p>
      </div>

      <div className="bg-white border border-gray-200 rounded-xl overflow-hidden">
        {loading ? (
          <p className="py-12 text-center text-gray-400 text-sm">Loading…</p>
        ) : appointments.length === 0 ? (
          <p className="py-12 text-center text-gray-400 text-sm">No appointments for this date.</p>
        ) : (
          <table className="w-full text-sm">
            <thead>
              <tr className="text-left text-xs text-gray-500 border-b border-gray-100 bg-gray-50">
                <th className="px-5 py-3 font-medium">Time</th>
                <th className="px-5 py-3 font-medium">Patient</th>
                <th className="px-5 py-3 font-medium">Doctor</th>
                <th className="px-5 py-3 font-medium">Reason</th>
                <th className="px-5 py-3 font-medium">Status</th>
                <th className="px-5 py-3 font-medium">Actions</th>
              </tr>
            </thead>
            <tbody>
              {appointments.map((a) => (
                <tr key={a.appointmentId} className="border-b border-gray-50 hover:bg-gray-50">
                  <td className="px-5 py-3 text-gray-700 font-medium">
                    {new Date(a.scheduledAt).toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit' })}
                  </td>
                  <td className="px-5 py-3">
                    <Link to={`/patients/${a.patientId}`} className="font-medium text-sky-700 hover:underline">
                      {a.patientName}
                    </Link>
                    <p className="text-xs text-gray-400 font-mono">{a.mrn}</p>
                  </td>
                  <td className="px-5 py-3 text-gray-600">{a.doctorName}</td>
                  <td className="px-5 py-3 text-gray-500">{a.reason ?? '—'}</td>
                  <td className="px-5 py-3">
                    <span className={`px-2 py-0.5 rounded text-xs font-medium ${APPOINTMENT_STATUS_COLORS[a.status] ?? 'bg-gray-100 text-gray-600'}`}>
                      {a.status}
                    </span>
                  </td>
                  <td className="px-5 py-3 flex gap-2">
                    {a.status === 'Scheduled' && (
                      <button onClick={() => handleAction(a.appointmentId, 'confirm')} className="text-green-600 hover:underline text-xs">Confirm</button>
                    )}
                    {(a.status === 'Scheduled' || a.status === 'Confirmed') && (
                      <button onClick={() => handleAction(a.appointmentId, 'checkin')} className="text-blue-600 hover:underline text-xs">Check In</button>
                    )}
                    {(a.status === 'Scheduled' || a.status === 'Confirmed') && (
                      <button onClick={() => handleAction(a.appointmentId, 'cancel')} className="text-red-500 hover:underline text-xs">Cancel</button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {showCreate && (
        <BookAppointmentModal
          doctors={doctors}
          onClose={() => setShowCreate(false)}
          onSave={async (data) => {
            await createAppointment(data)
            setShowCreate(false)
            load()
          }}
        />
      )}
    </div>
  )
}

function BookAppointmentModal({
  doctors,
  onClose,
  onSave,
}: {
  doctors: UserResponse[]
  onClose: () => void
  onSave: (data: CreateAppointmentRequest) => Promise<void>
}) {
  const [patientId, setPatientId] = useState('')
  const [patientQuery, setPatientQuery] = useState('')
  const [suggestions, setSuggestions] = useState<{ patientId: string; name: string; mrn: string }[]>([])
  const [doctorUserId, setDoctorUserId] = useState(doctors[0]?.userId ?? '')
  const [scheduledAt, setScheduledAt] = useState('')
  const [durationMinutes, setDurationMinutes] = useState(30)
  const [reason, setReason] = useState('')
  const [loading, setLoading] = useState(false)

  async function handlePatientSearch(q: string) {
    setPatientQuery(q)
    if (q.length < 2) { setSuggestions([]); return }
    const res = await searchPatients(q, 1, 6)
    setSuggestions(res.items.map((p) => ({ patientId: p.patientId, name: `${p.firstName} ${p.lastName}`, mrn: p.mrn })))
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!patientId) return
    setLoading(true)
    try {
      await onSave({ patientId, doctorUserId, scheduledAt, durationMinutes, reason: reason || undefined })
    } finally {
      setLoading(false)
    }
  }

  const inputCls = 'w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500'

  return (
    <div className="fixed inset-0 bg-black/30 flex items-center justify-center z-50">
      <div className="bg-white rounded-2xl shadow-xl p-6 w-full max-w-md">
        <h3 className="font-semibold text-gray-800 mb-4">Book Appointment</h3>
        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Patient</label>
            {patientId ? (
              <div className="flex items-center justify-between border border-gray-300 rounded-lg px-3 py-2">
                <span className="text-sm">{patientQuery}</span>
                <button type="button" onClick={() => { setPatientId(''); setPatientQuery('') }} className="text-xs text-red-500">Change</button>
              </div>
            ) : (
              <div className="relative">
                <input value={patientQuery} onChange={(e) => handlePatientSearch(e.target.value)} placeholder="Search patient…" className={inputCls} />
                {suggestions.length > 0 && (
                  <div className="absolute top-full mt-1 w-full bg-white border border-gray-200 rounded-lg shadow-lg z-10">
                    {suggestions.map((s) => (
                      <button key={s.patientId} type="button"
                        onClick={() => { setPatientId(s.patientId); setPatientQuery(`${s.name} (${s.mrn})`); setSuggestions([]) }}
                        className="w-full text-left px-3 py-2 text-sm hover:bg-gray-50 border-b last:border-0"
                      >
                        <span className="font-medium">{s.name}</span>{' '}
                        <span className="text-gray-400 font-mono text-xs">{s.mrn}</span>
                      </button>
                    ))}
                  </div>
                )}
              </div>
            )}
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Doctor</label>
            <select required value={doctorUserId} onChange={(e) => setDoctorUserId(e.target.value)} className={inputCls}>
              {doctors.map((d) => (
                <option key={d.userId} value={d.userId}>{d.firstName} {d.lastName}</option>
              ))}
            </select>
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Date & Time</label>
              <input required type="datetime-local" value={scheduledAt} onChange={(e) => setScheduledAt(e.target.value)} className={inputCls} />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Duration (min)</label>
              <input type="number" min="5" max="240" value={durationMinutes}
                onChange={(e) => setDurationMinutes(parseInt(e.target.value))} className={inputCls} />
            </div>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Reason</label>
            <input value={reason} onChange={(e) => setReason(e.target.value)} placeholder="Optional" className={inputCls} />
          </div>
          <div className="flex gap-3 pt-2">
            <button type="button" onClick={onClose} className="flex-1 border border-gray-300 rounded-lg py-2 text-sm hover:bg-gray-50">Cancel</button>
            <button type="submit" disabled={loading || !patientId}
              className="flex-1 bg-sky-700 text-white rounded-lg py-2 text-sm font-medium hover:bg-sky-800 disabled:opacity-60">
              {loading ? 'Booking…' : 'Book'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
