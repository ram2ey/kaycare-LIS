import { useEffect, useState, useCallback } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { searchPatients } from '../../api/patients'
import type { PatientSummary } from '../../types/patients'

export function PatientsPage() {
  const navigate = useNavigate()
  const [query, setQuery] = useState('')
  const [patients, setPatients] = useState<PatientSummary[]>([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [loading, setLoading] = useState(false)
  const pageSize = 20

  const load = useCallback((q: string, p: number) => {
    setLoading(true)
    searchPatients(q || undefined, p, pageSize)
      .then((r) => {
        setPatients(r.items)
        setTotal(r.total)
      })
      .finally(() => setLoading(false))
  }, [])

  useEffect(() => { load(query, page) }, [load, query, page])

  function handleSearch(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault()
    setPage(1)
    load(query, 1)
  }

  const totalPages = Math.ceil(total / pageSize)

  return (
    <div className="p-6">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Patients</h1>
          <p className="text-gray-500 text-sm mt-0.5">{total} registered patients</p>
        </div>
        <Link
          to="/patients/new"
          className="bg-sky-700 hover:bg-sky-800 text-white px-4 py-2 rounded-lg text-sm font-medium transition-colors"
        >
          + Register Patient
        </Link>
      </div>

      <form onSubmit={handleSearch} className="mb-4 flex gap-2">
        <input
          type="text"
          placeholder="Search by name, MRN, or phone…"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          className="flex-1 border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
        />
        <button
          type="submit"
          className="bg-sky-700 text-white px-4 py-2 rounded-lg text-sm hover:bg-sky-800 transition-colors"
        >
          Search
        </button>
      </form>

      <div className="bg-white border border-gray-200 rounded-xl overflow-hidden">
        {loading ? (
          <p className="py-12 text-center text-gray-400 text-sm">Loading…</p>
        ) : patients.length === 0 ? (
          <p className="py-12 text-center text-gray-400 text-sm">No patients found.</p>
        ) : (
          <table className="w-full text-sm">
            <thead>
              <tr className="text-left text-xs text-gray-500 border-b border-gray-100 bg-gray-50">
                <th className="px-5 py-3 font-medium">MRN</th>
                <th className="px-5 py-3 font-medium">Name</th>
                <th className="px-5 py-3 font-medium">DOB</th>
                <th className="px-5 py-3 font-medium">Gender</th>
                <th className="px-5 py-3 font-medium">Phone</th>
                <th className="px-5 py-3 font-medium">Actions</th>
              </tr>
            </thead>
            <tbody>
              {patients.map((p) => (
                <tr
                  key={p.patientId}
                  className="border-b border-gray-50 hover:bg-gray-50 cursor-pointer"
                  onClick={() => navigate(`/patients/${p.patientId}`)}
                >
                  <td className="px-5 py-3 font-mono text-xs text-sky-700">{p.mrn}</td>
                  <td className="px-5 py-3 font-medium text-gray-800">
                    {p.firstName} {p.lastName}
                  </td>
                  <td className="px-5 py-3 text-gray-500">
                    {new Date(p.dateOfBirth).toLocaleDateString('en-GB')}
                  </td>
                  <td className="px-5 py-3 text-gray-500">{p.gender}</td>
                  <td className="px-5 py-3 text-gray-500">{p.phone}</td>
                  <td className="px-5 py-3">
                    <Link
                      to={`/patients/${p.patientId}`}
                      className="text-sky-600 hover:underline text-xs"
                      onClick={(e) => e.stopPropagation()}
                    >
                      View →
                    </Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {totalPages > 1 && (
        <div className="flex items-center justify-between mt-4">
          <p className="text-sm text-gray-500">
            Page {page} of {totalPages}
          </p>
          <div className="flex gap-2">
            <button
              disabled={page === 1}
              onClick={() => setPage((p) => p - 1)}
              className="px-3 py-1.5 text-sm border border-gray-300 rounded-lg hover:bg-gray-50 disabled:opacity-50"
            >
              ← Prev
            </button>
            <button
              disabled={page === totalPages}
              onClick={() => setPage((p) => p + 1)}
              className="px-3 py-1.5 text-sm border border-gray-300 rounded-lg hover:bg-gray-50 disabled:opacity-50"
            >
              Next →
            </button>
          </div>
        </div>
      )}
    </div>
  )
}
