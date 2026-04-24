import { useEffect, useState } from 'react'
import { getDepartments, renameDepartment } from '../../api/users'

interface Dept {
  name: string
  userCount: number
}

export function DepartmentsPage() {
  const [depts, setDepts] = useState<Dept[]>([])
  const [renameTarget, setRenameTarget] = useState<Dept | null>(null)
  const [newName, setNewName] = useState('')
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)

  function load() {
    setLoading(true)
    getDepartments().then(setDepts).finally(() => setLoading(false))
  }

  useEffect(() => { load() }, [])

  async function handleRename(e: React.FormEvent) {
    e.preventDefault()
    if (!renameTarget) return
    setSaving(true)
    try {
      await renameDepartment(renameTarget.name, newName)
      setRenameTarget(null)
      setNewName('')
      load()
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="p-6 max-w-xl">
      <h1 className="text-2xl font-bold text-gray-900 mb-6">Departments</h1>

      <div className="bg-white border border-gray-200 rounded-xl overflow-hidden">
        {loading ? (
          <p className="py-12 text-center text-gray-400 text-sm">Loading…</p>
        ) : depts.length === 0 ? (
          <p className="py-12 text-center text-gray-400 text-sm">
            No departments yet. Departments are created from staff user profiles.
          </p>
        ) : (
          <table className="w-full text-sm">
            <thead>
              <tr className="text-left text-xs text-gray-500 border-b border-gray-100 bg-gray-50">
                <th className="px-5 py-3 font-medium">Department</th>
                <th className="px-5 py-3 font-medium">Staff</th>
                <th className="px-5 py-3 font-medium">Actions</th>
              </tr>
            </thead>
            <tbody>
              {depts.map((d) => (
                <tr key={d.name} className="border-b border-gray-50 hover:bg-gray-50">
                  <td className="px-5 py-3 font-medium text-gray-800">{d.name}</td>
                  <td className="px-5 py-3">
                    <span className="bg-sky-100 text-sky-700 px-2 py-0.5 rounded-full text-xs font-medium">
                      {d.userCount} staff
                    </span>
                  </td>
                  <td className="px-5 py-3">
                    <button
                      onClick={() => { setRenameTarget(d); setNewName(d.name) }}
                      className="text-sky-600 hover:underline text-xs"
                    >
                      Rename
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {renameTarget && (
        <div className="fixed inset-0 bg-black/30 flex items-center justify-center z-50">
          <div className="bg-white rounded-2xl shadow-xl p-6 w-full max-w-sm">
            <h3 className="font-semibold text-gray-800 mb-1">Rename Department</h3>
            <p className="text-sm text-gray-500 mb-4">
              This will update all {renameTarget.userCount} staff member{renameTarget.userCount !== 1 ? 's' : ''} in "{renameTarget.name}".
            </p>
            <form onSubmit={handleRename} className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">New Name</label>
                <input
                  required
                  value={newName}
                  onChange={(e) => setNewName(e.target.value)}
                  className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
                />
              </div>
              <div className="flex gap-3">
                <button
                  type="button"
                  onClick={() => setRenameTarget(null)}
                  className="flex-1 border border-gray-300 rounded-lg py-2 text-sm hover:bg-gray-50"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={saving}
                  className="flex-1 bg-sky-700 text-white rounded-lg py-2 text-sm font-medium hover:bg-sky-800 disabled:opacity-60"
                >
                  {saving ? 'Saving…' : 'Rename'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  )
}
