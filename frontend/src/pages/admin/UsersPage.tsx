import { useEffect, useState } from 'react'
import { getUsers, createUser, updateUser, deactivateUser, reactivateUser, resetPassword, getDepartments } from '../../api/users'
import type { UserResponse, CreateUserRequest } from '../../types/users'
import { ROLE_OPTIONS, ROLE_COLORS } from '../../types/users'

export function UsersPage() {
  const [users, setUsers] = useState<UserResponse[]>([])
  const [depts, setDepts] = useState<string[]>([])
  const [includeInactive, setIncludeInactive] = useState(false)
  const [showCreate, setShowCreate] = useState(false)
  const [editUser, setEditUser] = useState<UserResponse | null>(null)
  const [resetUser, setResetUser] = useState<UserResponse | null>(null)
  const [loading, setLoading] = useState(true)

  function load() {
    setLoading(true)
    Promise.all([
      getUsers({ includeInactive }).then(setUsers),
      getDepartments().then((d) => setDepts(d.map((x) => x.name))),
    ]).finally(() => setLoading(false))
  }

  useEffect(() => { load() }, [includeInactive])

  return (
    <div className="p-6">
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Staff Users</h1>
        <div className="flex gap-3 items-center">
          <label className="flex items-center gap-2 text-sm text-gray-600">
            <input type="checkbox" checked={includeInactive} onChange={(e) => setIncludeInactive(e.target.checked)} />
            Show inactive
          </label>
          <button
            onClick={() => setShowCreate(true)}
            className="bg-sky-700 hover:bg-sky-800 text-white px-4 py-2 rounded-lg text-sm font-medium"
          >
            + Add User
          </button>
        </div>
      </div>

      <div className="bg-white border border-gray-200 rounded-xl overflow-hidden">
        {loading ? (
          <p className="py-12 text-center text-gray-400 text-sm">Loading…</p>
        ) : (
          <table className="w-full text-sm">
            <thead>
              <tr className="text-left text-xs text-gray-500 border-b border-gray-100 bg-gray-50">
                <th className="px-5 py-3 font-medium">Name</th>
                <th className="px-5 py-3 font-medium">Email</th>
                <th className="px-5 py-3 font-medium">Role</th>
                <th className="px-5 py-3 font-medium">Department</th>
                <th className="px-5 py-3 font-medium">Status</th>
                <th className="px-5 py-3 font-medium">Actions</th>
              </tr>
            </thead>
            <tbody>
              {users.map((u) => (
                <tr key={u.userId} className="border-b border-gray-50 hover:bg-gray-50">
                  <td className="px-5 py-3 font-medium text-gray-800">
                    {u.firstName} {u.lastName}
                    {u.mustChangePassword && (
                      <span className="ml-2 text-xs bg-orange-100 text-orange-700 px-1.5 py-0.5 rounded">pwd reset</span>
                    )}
                  </td>
                  <td className="px-5 py-3 text-gray-500">{u.email}</td>
                  <td className="px-5 py-3">
                    <span className={`px-2 py-0.5 rounded text-xs font-medium ${ROLE_COLORS[u.roleId] ?? 'bg-gray-100 text-gray-600'}`}>
                      {u.roleName}
                    </span>
                  </td>
                  <td className="px-5 py-3 text-gray-500">{u.department ?? '—'}</td>
                  <td className="px-5 py-3">
                    <span className={`px-2 py-0.5 rounded text-xs font-medium ${u.isActive ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500'}`}>
                      {u.isActive ? 'Active' : 'Inactive'}
                    </span>
                  </td>
                  <td className="px-5 py-3 flex gap-3">
                    <button onClick={() => setEditUser(u)} className="text-sky-600 hover:underline text-xs">Edit</button>
                    <button onClick={() => setResetUser(u)} className="text-orange-500 hover:underline text-xs">Reset pwd</button>
                    {u.isActive ? (
                      <button onClick={async () => { await deactivateUser(u.userId); load() }} className="text-red-500 hover:underline text-xs">Deactivate</button>
                    ) : (
                      <button onClick={async () => { await reactivateUser(u.userId); load() }} className="text-green-600 hover:underline text-xs">Reactivate</button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {showCreate && (
        <UserModal
          depts={depts}
          onClose={() => setShowCreate(false)}
          onSave={async (data) => {
            await createUser(data)
            setShowCreate(false)
            load()
          }}
        />
      )}

      {editUser && (
        <UserModal
          user={editUser}
          depts={depts}
          onClose={() => setEditUser(null)}
          onSave={async (data) => {
            await updateUser(editUser.userId, data)
            setEditUser(null)
            load()
          }}
        />
      )}

      {resetUser && (
        <ResetPasswordModal
          user={resetUser}
          onClose={() => setResetUser(null)}
          onSave={async (pwd) => {
            await resetPassword(resetUser.userId, { newPassword: pwd })
            setResetUser(null)
          }}
        />
      )}
    </div>
  )
}

function UserModal({
  user,
  depts,
  onClose,
  onSave,
}: {
  user?: UserResponse
  depts: string[]
  onClose: () => void
  onSave: (data: CreateUserRequest) => Promise<void>
}) {
  const [form, setForm] = useState({
    firstName: user?.firstName ?? '',
    lastName: user?.lastName ?? '',
    email: user?.email ?? '',
    roleId: user?.roleId ?? 3,
    department: user?.department ?? '',
    phone: user?.phone ?? '',
  })
  const [loading, setLoading] = useState(false)

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setLoading(true)
    try {
      await onSave(form)
    } finally {
      setLoading(false)
    }
  }

  const inputCls = 'w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500'

  return (
    <div className="fixed inset-0 bg-black/30 flex items-center justify-center z-50">
      <div className="bg-white rounded-2xl shadow-xl p-6 w-full max-w-md">
        <h3 className="font-semibold text-gray-800 mb-4">{user ? 'Edit User' : 'Add User'}</h3>
        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">First Name</label>
              <input required value={form.firstName} onChange={(e) => setForm({ ...form, firstName: e.target.value })} className={inputCls} />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Last Name</label>
              <input required value={form.lastName} onChange={(e) => setForm({ ...form, lastName: e.target.value })} className={inputCls} />
            </div>
          </div>
          {!user && (
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Email</label>
              <input required type="email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} className={inputCls} />
            </div>
          )}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Role</label>
            <select value={form.roleId} onChange={(e) => setForm({ ...form, roleId: parseInt(e.target.value) })} className={inputCls}>
              {ROLE_OPTIONS.map((r) => <option key={r.id} value={r.id}>{r.name}</option>)}
            </select>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Department</label>
            <input list="dept-list" value={form.department} onChange={(e) => setForm({ ...form, department: e.target.value })} className={inputCls} />
            <datalist id="dept-list">
              {depts.map((d) => <option key={d} value={d} />)}
            </datalist>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Phone</label>
            <input value={form.phone} onChange={(e) => setForm({ ...form, phone: e.target.value })} className={inputCls} />
          </div>
          <div className="flex gap-3 pt-2">
            <button type="button" onClick={onClose} className="flex-1 border border-gray-300 rounded-lg py-2 text-sm hover:bg-gray-50">Cancel</button>
            <button type="submit" disabled={loading} className="flex-1 bg-sky-700 text-white rounded-lg py-2 text-sm font-medium hover:bg-sky-800 disabled:opacity-60">
              {loading ? 'Saving…' : 'Save'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}

function ResetPasswordModal({
  user,
  onClose,
  onSave,
}: {
  user: UserResponse
  onClose: () => void
  onSave: (pwd: string) => Promise<void>
}) {
  const [pwd, setPwd] = useState('')
  const [loading, setLoading] = useState(false)

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setLoading(true)
    try {
      await onSave(pwd)
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="fixed inset-0 bg-black/30 flex items-center justify-center z-50">
      <div className="bg-white rounded-2xl shadow-xl p-6 w-full max-w-sm">
        <h3 className="font-semibold text-gray-800 mb-1">Reset Password</h3>
        <p className="text-sm text-gray-500 mb-4">{user.firstName} {user.lastName}</p>
        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">New Password</label>
            <input required type="password" minLength={8} value={pwd} onChange={(e) => setPwd(e.target.value)}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
            />
          </div>
          <div className="flex gap-3">
            <button type="button" onClick={onClose} className="flex-1 border border-gray-300 rounded-lg py-2 text-sm hover:bg-gray-50">Cancel</button>
            <button type="submit" disabled={loading} className="flex-1 bg-orange-500 text-white rounded-lg py-2 text-sm font-medium hover:bg-orange-600 disabled:opacity-60">
              {loading ? 'Saving…' : 'Reset'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
