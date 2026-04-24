import { useEffect, useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { getLabOrdersForPatient } from '../../api/labOrders'
import { searchPatients } from '../../api/patients'
import type { LabOrderSummary } from '../../types/labOrders'
import { ORDER_STATUS_COLORS } from '../../types/labOrders'

export function LabOrdersPage() {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const patientIdParam = searchParams.get('patientId')
  const [patientQuery, setPatientQuery] = useState('')
  const [selectedPatientId, setSelectedPatientId] = useState(patientIdParam ?? '')
  const [orders, setOrders] = useState<LabOrderSummary[]>([])
  const [suggestions, setSuggestions] = useState<{ patientId: string; name: string; mrn: string }[]>([])
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    if (selectedPatientId) {
      setLoading(true)
      getLabOrdersForPatient(selectedPatientId).then(setOrders).finally(() => setLoading(false))
    }
  }, [selectedPatientId])

  async function handlePatientSearch(q: string) {
    setPatientQuery(q)
    if (q.length < 2) { setSuggestions([]); return }
    const res = await searchPatients(q, 1, 6)
    setSuggestions(res.items.map((p) => ({ patientId: p.patientId, name: `${p.firstName} ${p.lastName}`, mrn: p.mrn })))
  }

  return (
    <div className="p-6">
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Lab Orders</h1>
        <Link
          to="/lab-orders/new"
          className="bg-sky-700 hover:bg-sky-800 text-white px-4 py-2 rounded-lg text-sm font-medium transition-colors"
        >
          + New Lab Order
        </Link>
      </div>

      {/* Patient search */}
      <div className="relative mb-6 max-w-sm">
        <input
          type="text"
          placeholder="Search patient by name or MRN…"
          value={patientQuery}
          onChange={(e) => handlePatientSearch(e.target.value)}
          className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
        />
        {suggestions.length > 0 && (
          <div className="absolute top-full mt-1 w-full bg-white border border-gray-200 rounded-lg shadow-lg z-10">
            {suggestions.map((s) => (
              <button
                key={s.patientId}
                onClick={() => {
                  setSelectedPatientId(s.patientId)
                  setPatientQuery(`${s.name} (${s.mrn})`)
                  setSuggestions([])
                }}
                className="w-full text-left px-3 py-2 text-sm hover:bg-gray-50 border-b border-gray-50 last:border-0"
              >
                <span className="font-medium">{s.name}</span>{' '}
                <span className="text-gray-400 font-mono text-xs">{s.mrn}</span>
              </button>
            ))}
          </div>
        )}
      </div>

      {!selectedPatientId ? (
        <p className="text-gray-400 text-sm py-12 text-center">Select a patient to view their lab orders.</p>
      ) : loading ? (
        <p className="text-gray-400 text-sm">Loading…</p>
      ) : orders.length === 0 ? (
        <p className="text-gray-400 text-sm py-12 text-center">No lab orders for this patient.</p>
      ) : (
        <div className="bg-white border border-gray-200 rounded-xl overflow-hidden">
          <table className="w-full text-sm">
            <thead>
              <tr className="text-left text-xs text-gray-500 border-b border-gray-100 bg-gray-50">
                <th className="px-5 py-3 font-medium">Accession</th>
                <th className="px-5 py-3 font-medium">Tests</th>
                <th className="px-5 py-3 font-medium">Priority</th>
                <th className="px-5 py-3 font-medium">Status</th>
                <th className="px-5 py-3 font-medium">Ordered</th>
              </tr>
            </thead>
            <tbody>
              {orders.map((o) => (
                <tr
                  key={o.labOrderId}
                  className="border-b border-gray-50 hover:bg-gray-50 cursor-pointer"
                  onClick={() => navigate(`/lab-orders/${o.labOrderId}`)}
                >
                  <td className="px-5 py-3 font-mono text-xs text-sky-700">{o.accessionNumber}</td>
                  <td className="px-5 py-3 text-gray-600 text-xs">{o.items.map((i) => i.testName).join(', ')}</td>
                  <td className="px-5 py-3">
                    <span className={`px-2 py-0.5 rounded text-xs font-medium ${
                      o.priority === 'STAT' ? 'bg-red-100 text-red-700' :
                      o.priority === 'Urgent' ? 'bg-orange-100 text-orange-700' :
                      'bg-gray-100 text-gray-600'
                    }`}>{o.priority}</span>
                  </td>
                  <td className="px-5 py-3">
                    <span className={`px-2 py-0.5 rounded text-xs font-medium ${ORDER_STATUS_COLORS[o.status] ?? 'bg-gray-100 text-gray-600'}`}>
                      {o.status}
                    </span>
                  </td>
                  <td className="px-5 py-3 text-gray-500 text-xs">
                    {new Date(o.createdAt).toLocaleDateString('en-GB')}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
