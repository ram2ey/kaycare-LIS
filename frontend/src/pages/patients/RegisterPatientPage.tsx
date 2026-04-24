import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { registerPatient } from '../../api/patients'
import { GENDER_OPTIONS, BLOOD_GROUP_OPTIONS } from '../../types/patients'

export function RegisterPatientPage() {
  const navigate = useNavigate()
  const [form, setForm] = useState({
    firstName: '',
    lastName: '',
    dateOfBirth: '',
    gender: '',
    phone: '',
    email: '',
    address: '',
    bloodGroup: '',
    nhisNumber: '',
    emergencyContactName: '',
    emergencyContactPhone: '',
    notes: '',
  })
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  function set(field: string, value: string) {
    setForm((f) => ({ ...f, [field]: value }))
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    setLoading(true)
    try {
      const patient = await registerPatient({
        firstName: form.firstName,
        lastName: form.lastName,
        dateOfBirth: form.dateOfBirth,
        gender: form.gender,
        phone: form.phone,
        email: form.email || undefined,
        address: form.address || undefined,
        bloodGroup: form.bloodGroup || undefined,
        nhisNumber: form.nhisNumber || undefined,
        emergencyContactName: form.emergencyContactName || undefined,
        emergencyContactPhone: form.emergencyContactPhone || undefined,
        notes: form.notes || undefined,
      })
      navigate(`/patients/${patient.patientId}`)
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })
        ?.response?.data?.message
      setError(msg ?? 'Failed to register patient.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="p-6 max-w-2xl">
      <div className="mb-6">
        <button
          onClick={() => navigate(-1)}
          className="text-sm text-gray-500 hover:text-gray-700 mb-2"
        >
          ← Back
        </button>
        <h1 className="text-2xl font-bold text-gray-900">Register Patient</h1>
      </div>

      <form onSubmit={handleSubmit} className="bg-white border border-gray-200 rounded-xl p-6 space-y-5">
        <div className="grid grid-cols-2 gap-4">
          <Field label="First Name" required>
            <input
              required
              value={form.firstName}
              onChange={(e) => set('firstName', e.target.value)}
              className={inputCls}
            />
          </Field>
          <Field label="Last Name" required>
            <input
              required
              value={form.lastName}
              onChange={(e) => set('lastName', e.target.value)}
              className={inputCls}
            />
          </Field>
        </div>

        <div className="grid grid-cols-2 gap-4">
          <Field label="Date of Birth" required>
            <input
              type="date"
              required
              value={form.dateOfBirth}
              onChange={(e) => set('dateOfBirth', e.target.value)}
              className={inputCls}
            />
          </Field>
          <Field label="Gender" required>
            <select
              required
              value={form.gender}
              onChange={(e) => set('gender', e.target.value)}
              className={inputCls}
            >
              <option value="">Select…</option>
              {GENDER_OPTIONS.map((g) => <option key={g}>{g}</option>)}
            </select>
          </Field>
        </div>

        <div className="grid grid-cols-2 gap-4">
          <Field label="Phone" required>
            <input
              required
              value={form.phone}
              onChange={(e) => set('phone', e.target.value)}
              className={inputCls}
            />
          </Field>
          <Field label="Email">
            <input
              type="email"
              value={form.email}
              onChange={(e) => set('email', e.target.value)}
              className={inputCls}
            />
          </Field>
        </div>

        <Field label="Address">
          <input value={form.address} onChange={(e) => set('address', e.target.value)} className={inputCls} />
        </Field>

        <div className="grid grid-cols-2 gap-4">
          <Field label="Blood Group">
            <select value={form.bloodGroup} onChange={(e) => set('bloodGroup', e.target.value)} className={inputCls}>
              <option value="">Unknown</option>
              {BLOOD_GROUP_OPTIONS.map((b) => <option key={b}>{b}</option>)}
            </select>
          </Field>
          <Field label="NHIS Number">
            <input value={form.nhisNumber} onChange={(e) => set('nhisNumber', e.target.value)} className={inputCls} />
          </Field>
        </div>

        <div className="grid grid-cols-2 gap-4">
          <Field label="Emergency Contact Name">
            <input value={form.emergencyContactName} onChange={(e) => set('emergencyContactName', e.target.value)} className={inputCls} />
          </Field>
          <Field label="Emergency Contact Phone">
            <input value={form.emergencyContactPhone} onChange={(e) => set('emergencyContactPhone', e.target.value)} className={inputCls} />
          </Field>
        </div>

        <Field label="Notes">
          <textarea
            rows={2}
            value={form.notes}
            onChange={(e) => set('notes', e.target.value)}
            className={inputCls}
          />
        </Field>

        {error && (
          <p className="text-red-600 text-sm bg-red-50 border border-red-200 rounded-lg px-3 py-2">{error}</p>
        )}

        <div className="flex gap-3 pt-2">
          <button
            type="button"
            onClick={() => navigate(-1)}
            className="px-4 py-2 border border-gray-300 rounded-lg text-sm hover:bg-gray-50"
          >
            Cancel
          </button>
          <button
            type="submit"
            disabled={loading}
            className="bg-sky-700 hover:bg-sky-800 text-white px-6 py-2 rounded-lg text-sm font-medium disabled:opacity-60"
          >
            {loading ? 'Registering…' : 'Register Patient'}
          </button>
        </div>
      </form>
    </div>
  )
}

const inputCls =
  'w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500'

function Field({ label, required, children }: { label: string; required?: boolean; children: React.ReactNode }) {
  return (
    <div>
      <label className="block text-sm font-medium text-gray-700 mb-1">
        {label}
        {required && <span className="text-red-500 ml-0.5">*</span>}
      </label>
      {children}
    </div>
  )
}
