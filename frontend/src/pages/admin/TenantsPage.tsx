import { useEffect, useState } from 'react'
import { getTenants, createTenant, updateTenant, setTenantActive, deleteTenant } from '../../api/tenants'
import type { TenantResponse, CreateTenantRequest, UpdateTenantRequest } from '../../types/tenants'

export function TenantsPage() {
  const [tenants, setTenants] = useState<TenantResponse[]>([])
  const [showCreate, setShowCreate] = useState(false)
  const [editTenant, setEditTenant] = useState<TenantResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  function load() {
    setLoading(true)
    setError(null)
    getTenants()
      .then(setTenants)
      .catch((err) => {
        console.error(err)
        setError('Failed to fetch tenants. Please ensure you are logged in as a SuperAdmin.')
      })
      .finally(() => setLoading(false))
  }

  useEffect(() => {
    load()
  }, [])

  async function handleToggleActive(t: TenantResponse) {
    try {
      await setTenantActive(t.tenantId, !t.isActive)
      load()
    } catch (err) {
      console.error(err)
      alert('Failed to update tenant status.')
    }
  }

  async function handleDelete(t: TenantResponse) {
    if (!confirm(`Are you absolutely sure you want to delete tenant "${t.tenantName}"? This action is permanent and cannot be undone.`)) {
      return
    }
    try {
      await deleteTenant(t.tenantId)
      load()
    } catch (err) {
      console.error(err)
      alert('Failed to delete tenant.')
    }
  }

  return (
    <div className="p-6">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Tenants</h1>
          <p className="text-sm text-gray-500 mt-1">Manage platform-level tenants, subscription plans, and resource limits.</p>
        </div>
        <button
          onClick={() => setShowCreate(true)}
          className="bg-sky-700 hover:bg-sky-800 text-white px-4 py-2 rounded-lg text-sm font-medium transition-colors"
        >
          + Add Tenant
        </button>
      </div>

      {error && (
        <div className="mb-6 p-4 bg-red-50 border border-red-100 rounded-xl text-sm text-red-600">
          {error}
        </div>
      )}

      <div className="bg-white border border-gray-200 rounded-xl overflow-hidden shadow-sm">
        {loading ? (
          <p className="py-12 text-center text-gray-400 text-sm">Loading tenants…</p>
        ) : tenants.length === 0 ? (
          <p className="py-12 text-center text-gray-400 text-sm">No tenants found.</p>
        ) : (
          <table className="w-full text-sm">
            <thead>
              <tr className="text-left text-xs text-gray-500 border-b border-gray-100 bg-gray-50/50">
                <th className="px-5 py-3.5 font-medium">Tenant Details</th>
                <th className="px-5 py-3.5 font-medium">Subdomain</th>
                <th className="px-5 py-3.5 font-medium">Subscription</th>
                <th className="px-5 py-3.5 font-medium">Limits</th>
                <th className="px-5 py-3.5 font-medium">Status</th>
                <th className="px-5 py-3.5 font-medium">Actions</th>
              </tr>
            </thead>
            <tbody>
              {tenants.map((t) => (
                <tr key={t.tenantId} className="border-b border-gray-50 hover:bg-gray-50/40 transition-colors">
                  <td className="px-5 py-4">
                    <p className="font-semibold text-gray-800">{t.tenantName}</p>
                    <p className="text-xs text-gray-400 font-mono mt-0.5">{t.tenantCode}</p>
                  </td>
                  <td className="px-5 py-4 text-gray-600 font-medium">{t.subdomain}</td>
                  <td className="px-5 py-4">
                    <span className="px-2.5 py-0.5 rounded-full text-xs font-semibold bg-sky-50 text-sky-700 border border-sky-100">
                      {t.subscriptionPlan}
                    </span>
                  </td>
                  <td className="px-5 py-4 text-gray-500">
                    <div className="text-xs space-y-0.5">
                      <p><span className="font-semibold text-gray-700">{t.maxUsers}</span> Max Users</p>
                      <p><span className="font-semibold text-gray-700">{t.storageQuotaGB} GB</span> Storage</p>
                    </div>
                  </td>
                  <td className="px-5 py-4">
                    <span className={`inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-semibold ${t.isActive ? 'bg-green-50 text-green-700 border border-green-100' : 'bg-gray-50 text-gray-500 border border-gray-150'}`}>
                      <span className={`w-1.5 h-1.5 rounded-full ${t.isActive ? 'bg-green-500' : 'bg-gray-400'}`}></span>
                      {t.isActive ? 'Active' : 'Suspended'}
                    </span>
                  </td>
                  <td className="px-5 py-4">
                    <div className="flex gap-4">
                      <button onClick={() => setEditTenant(t)} className="text-sky-600 hover:text-sky-800 hover:underline text-xs font-medium">
                        Edit
                      </button>
                      <button
                        onClick={() => handleToggleActive(t)}
                        className={`text-xs font-medium hover:underline ${t.isActive ? 'text-amber-600 hover:text-amber-800' : 'text-green-600 hover:text-green-800'}`}
                      >
                        {t.isActive ? 'Suspend' : 'Activate'}
                      </button>
                      <button onClick={() => handleDelete(t)} className="text-red-500 hover:text-red-700 hover:underline text-xs font-medium">
                        Delete
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {showCreate && (
        <CreateTenantModal
          onClose={() => setShowCreate(false)}
          onSave={async (data) => {
            await createTenant(data)
            setShowCreate(false)
            load()
          }}
        />
      )}

      {editTenant && (
        <EditTenantModal
          tenant={editTenant}
          onClose={() => setEditTenant(null)}
          onSave={async (data) => {
            await updateTenant(editTenant.tenantId, data)
            setEditTenant(null)
            load()
          }}
        />
      )}
    </div>
  )
}

function CreateTenantModal({
  onClose,
  onSave,
}: {
  onClose: () => void
  onSave: (data: CreateTenantRequest) => Promise<void>
}) {
  const [form, setForm] = useState<CreateTenantRequest>({
    tenantCode: '',
    tenantName: '',
    subscriptionPlan: 'Standard',
    maxUsers: 50,
    storageQuotaGB: 100,
    adminEmail: '',
    adminFirstName: '',
    adminLastName: '',
  })
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setLoading(true)
    try {
      await onSave(form)
    } catch (err: any) {
      console.error(err)
      setError(err?.response?.data?.error || 'Failed to create tenant.')
    } finally {
      setLoading(false)
    }
  }

  const labelCls = 'block text-xs font-bold uppercase tracking-wider text-gray-500 mb-1'
  const inputCls = 'w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500 transition-shadow'

  return (
    <div className="fixed inset-0 bg-black/40 backdrop-blur-xs flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-2xl shadow-xl w-full max-w-lg overflow-hidden animate-page-entry">
        <div className="bg-sky-800 px-6 py-4 text-white">
          <h3 className="font-bold text-lg">Create New Tenant</h3>
          <p className="text-xs text-sky-200 mt-1">Register a new laboratory/clinic on the platform</p>
        </div>

        <form onSubmit={handleSubmit} className="p-6 space-y-4 max-h-[80vh] overflow-y-auto">
          {error && (
            <div className="p-3 bg-red-50 border border-red-100 rounded-lg text-xs text-red-600">
              {error}
            </div>
          )}

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className={labelCls}>Tenant Name</label>
              <input
                required
                value={form.tenantName}
                onChange={(e) => setForm({ ...form, tenantName: e.target.value })}
                placeholder="KayCare General Hospital"
                className={inputCls}
              />
            </div>
            <div>
              <label className={labelCls}>Tenant Code (Subdomain)</label>
              <input
                required
                value={form.tenantCode}
                onChange={(e) => setForm({ ...form, tenantCode: e.target.value.toLowerCase().replace(/[^a-z0-9-]/g, '') })}
                placeholder="kaycare-general"
                className={inputCls}
              />
            </div>
          </div>

          <div className="grid grid-cols-3 gap-3">
            <div>
              <label className={labelCls}>Subscription</label>
              <select
                value={form.subscriptionPlan}
                onChange={(e) => setForm({ ...form, subscriptionPlan: e.target.value })}
                className={inputCls}
              >
                <option value="Basic">Basic</option>
                <option value="Standard">Standard</option>
                <option value="Enterprise">Enterprise</option>
              </select>
            </div>
            <div>
              <label className={labelCls}>Max Users</label>
              <input
                type="number"
                min={1}
                required
                value={form.maxUsers}
                onChange={(e) => setForm({ ...form, maxUsers: parseInt(e.target.value) || 0 })}
                className={inputCls}
              />
            </div>
            <div>
              <label className={labelCls}>Storage (GB)</label>
              <input
                type="number"
                min={1}
                required
                value={form.storageQuotaGB}
                onChange={(e) => setForm({ ...form, storageQuotaGB: parseInt(e.target.value) || 0 })}
                className={inputCls}
              />
            </div>
          </div>

          <div className="border-t border-gray-100 pt-4">
            <h4 className="text-xs font-bold text-sky-800 uppercase tracking-wider mb-3">Initial Administrator Account</h4>
            <div className="space-y-3">
              <div>
                <label className={labelCls}>Admin Email</label>
                <input
                  required
                  type="email"
                  value={form.adminEmail}
                  onChange={(e) => setForm({ ...form, adminEmail: e.target.value })}
                  placeholder="admin@hospital.com"
                  className={inputCls}
                />
              </div>
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className={labelCls}>First Name</label>
                  <input
                    required
                    value={form.adminFirstName}
                    onChange={(e) => setForm({ ...form, adminFirstName: e.target.value })}
                    placeholder="John"
                    className={inputCls}
                  />
                </div>
                <div>
                  <label className={labelCls}>Last Name</label>
                  <input
                    required
                    value={form.adminLastName}
                    onChange={(e) => setForm({ ...form, adminLastName: e.target.value })}
                    placeholder="Doe"
                    className={inputCls}
                  />
                </div>
              </div>
              <p className="text-[11px] text-amber-600 bg-amber-50 border border-amber-100 p-2.5 rounded-lg leading-relaxed">
                <strong>Temporary credentials:</strong> The administrator password will default to <code>Admin@1234</code>. They will be prompted to change it upon first login.
              </p>
            </div>
          </div>

          <div className="flex gap-3 pt-3 border-t border-gray-100">
            <button
              type="button"
              onClick={onClose}
              className="flex-1 border border-gray-300 rounded-lg py-2.5 text-sm font-semibold hover:bg-gray-50 transition-colors"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={loading}
              className="flex-1 bg-sky-700 text-white rounded-lg py-2.5 text-sm font-semibold hover:bg-sky-800 disabled:opacity-60 transition-colors"
            >
              {loading ? 'Creating Tenant…' : 'Create Tenant'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}

function EditTenantModal({
  tenant,
  onClose,
  onSave,
}: {
  tenant: TenantResponse
  onClose: () => void
  onSave: (data: UpdateTenantRequest) => Promise<void>
}) {
  const [form, setForm] = useState<UpdateTenantRequest>({
    tenantName: tenant.tenantName,
    subscriptionPlan: tenant.subscriptionPlan,
    maxUsers: tenant.maxUsers,
    storageQuotaGB: tenant.storageQuotaGB,
  })
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setLoading(true)
    try {
      await onSave(form)
    } catch (err: any) {
      console.error(err)
      setError(err?.response?.data?.error || 'Failed to update tenant.')
    } finally {
      setLoading(false)
    }
  }

  const labelCls = 'block text-xs font-bold uppercase tracking-wider text-gray-500 mb-1'
  const inputCls = 'w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500 transition-shadow'

  return (
    <div className="fixed inset-0 bg-black/40 backdrop-blur-xs flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-2xl shadow-xl w-full max-w-md overflow-hidden animate-page-entry">
        <div className="bg-sky-800 px-6 py-4 text-white">
          <h3 className="font-bold text-lg">Edit Tenant Settings</h3>
          <p className="text-xs text-sky-200 mt-1">{tenant.tenantName} ({tenant.tenantCode})</p>
        </div>

        <form onSubmit={handleSubmit} className="p-6 space-y-4">
          {error && (
            <div className="p-3 bg-red-50 border border-red-100 rounded-lg text-xs text-red-600">
              {error}
            </div>
          )}

          <div>
            <label className={labelCls}>Tenant Name</label>
            <input
              required
              value={form.tenantName}
              onChange={(e) => setForm({ ...form, tenantName: e.target.value })}
              className={inputCls}
            />
          </div>

          <div className="grid grid-cols-3 gap-3">
            <div>
              <label className={labelCls}>Subscription</label>
              <select
                value={form.subscriptionPlan}
                onChange={(e) => setForm({ ...form, subscriptionPlan: e.target.value })}
                className={inputCls}
              >
                <option value="Basic">Basic</option>
                <option value="Standard">Standard</option>
                <option value="Enterprise">Enterprise</option>
              </select>
            </div>
            <div>
              <label className={labelCls}>Max Users</label>
              <input
                type="number"
                min={1}
                required
                value={form.maxUsers}
                onChange={(e) => setForm({ ...form, maxUsers: parseInt(e.target.value) || 0 })}
                className={inputCls}
              />
            </div>
            <div>
              <label className={labelCls}>Storage (GB)</label>
              <input
                type="number"
                min={1}
                required
                value={form.storageQuotaGB}
                onChange={(e) => setForm({ ...form, storageQuotaGB: parseInt(e.target.value) || 0 })}
                className={inputCls}
              />
            </div>
          </div>

          <div className="flex gap-3 pt-3 border-t border-gray-100">
            <button
              type="button"
              onClick={onClose}
              className="flex-1 border border-gray-300 rounded-lg py-2.5 text-sm font-semibold hover:bg-gray-50 transition-colors"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={loading}
              className="flex-1 bg-sky-700 text-white rounded-lg py-2.5 text-sm font-semibold hover:bg-sky-800 disabled:opacity-60 transition-colors"
            >
              {loading ? 'Saving…' : 'Save Changes'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
