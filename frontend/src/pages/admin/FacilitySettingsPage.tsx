import { useEffect, useState } from 'react'
import { getFacilitySettings, saveFacilitySettings, uploadLogo, deleteLogo } from '../../api/facility'
import type { FacilitySettingsResponse } from '../../types/facility'

export function FacilitySettingsPage() {
  const [settings, setSettings] = useState<FacilitySettingsResponse | null>(null)
  const [form, setForm] = useState({ facilityName: '', address: '', phone: '', email: '' })
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [success, setSuccess] = useState(false)

  useEffect(() => {
    getFacilitySettings()
      .then((s) => {
        setSettings(s)
        setForm({
          facilityName: s.facilityName,
          address: s.address ?? '',
          phone: s.phone ?? '',
          email: s.email ?? '',
        })
      })
      .finally(() => setLoading(false))
  }, [])

  async function handleSave(e: React.FormEvent) {
    e.preventDefault()
    setSaving(true)
    try {
      const updated = await saveFacilitySettings({
        facilityName: form.facilityName,
        address: form.address || undefined,
        phone: form.phone || undefined,
        email: form.email || undefined,
      })
      setSettings(updated)
      setSuccess(true)
      setTimeout(() => setSuccess(false), 3000)
    } finally {
      setSaving(false)
    }
  }

  async function handleLogoUpload(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0]
    if (!file) return
    const updated = await uploadLogo(file)
    setSettings(updated)
    e.target.value = ''
  }

  async function handleDeleteLogo() {
    if (!confirm('Remove logo?')) return
    await deleteLogo()
    setSettings((s) => s ? { ...s, logoUrl: null } : s)
  }

  const inputCls = 'w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500'

  if (loading) return <div className="p-6 text-gray-400 text-sm">Loading…</div>

  return (
    <div className="p-6 max-w-xl">
      <h1 className="text-2xl font-bold text-gray-900 mb-6">Facility Settings</h1>

      {/* Logo */}
      <div className="bg-white border border-gray-200 rounded-xl p-6 mb-6">
        <h2 className="font-semibold text-gray-700 mb-4">Facility Logo</h2>
        <div className="flex items-center gap-4">
          {settings?.logoUrl ? (
            <img src={settings.logoUrl} alt="Facility Logo" className="h-16 object-contain rounded border border-gray-200 p-1" />
          ) : (
            <div className="h-16 w-32 bg-gray-100 rounded border border-dashed border-gray-300 flex items-center justify-center">
              <span className="text-xs text-gray-400">No logo</span>
            </div>
          )}
          <div className="flex flex-col gap-2">
            <label className="bg-sky-700 text-white px-3 py-1.5 rounded-lg text-sm cursor-pointer hover:bg-sky-800 transition-colors">
              Upload Logo
              <input type="file" accept="image/png,image/jpeg" className="hidden" onChange={handleLogoUpload} />
            </label>
            {settings?.logoUrl && (
              <button onClick={handleDeleteLogo} className="text-red-500 hover:underline text-sm">Remove</button>
            )}
            <p className="text-xs text-gray-400">PNG or JPEG, max 2 MB</p>
          </div>
        </div>
      </div>

      {/* Details form */}
      <div className="bg-white border border-gray-200 rounded-xl p-6">
        <h2 className="font-semibold text-gray-700 mb-4">Facility Details</h2>
        <form onSubmit={handleSave} className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Facility Name <span className="text-red-500">*</span></label>
            <input required value={form.facilityName} onChange={(e) => setForm({ ...form, facilityName: e.target.value })} className={inputCls} />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Address</label>
            <textarea rows={2} value={form.address} onChange={(e) => setForm({ ...form, address: e.target.value })} className={inputCls} />
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Phone</label>
              <input value={form.phone} onChange={(e) => setForm({ ...form, phone: e.target.value })} className={inputCls} />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Email</label>
              <input type="email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} className={inputCls} />
            </div>
          </div>

          {success && (
            <div className="bg-green-50 border border-green-200 rounded-lg px-3 py-2 text-sm text-green-700">
              Settings saved successfully.
            </div>
          )}

          <button
            type="submit"
            disabled={saving}
            className="bg-sky-700 hover:bg-sky-800 text-white px-6 py-2 rounded-lg text-sm font-medium disabled:opacity-60"
          >
            {saving ? 'Saving…' : 'Save Settings'}
          </button>
        </form>
      </div>
    </div>
  )
}
