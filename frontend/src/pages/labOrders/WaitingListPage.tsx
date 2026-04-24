import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { getWaitingList } from '../../api/labOrders'
import type { LabOrderSummary } from '../../types/labOrders'

export function WaitingListPage() {
  const [orders, setOrders] = useState<LabOrderSummary[]>([])
  const [loading, setLoading] = useState(true)

  function load() {
    setLoading(true)
    getWaitingList().then(setOrders).finally(() => setLoading(false))
  }

  useEffect(() => { load() }, [])

  const byStatus = {
    Ordered: orders.filter((o) => o.status === 'Ordered'),
    SampleCollected: orders.filter((o) => o.status === 'SampleCollected'),
    InProgress: orders.filter((o) => o.status === 'InProgress'),
    PartiallyCompleted: orders.filter((o) => o.status === 'PartiallyCompleted'),
  }

  return (
    <div className="p-6">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Waiting List</h1>
          <p className="text-gray-500 text-sm mt-0.5">{orders.length} active order{orders.length !== 1 ? 's' : ''}</p>
        </div>
        <button
          onClick={load}
          className="text-sm text-sky-600 hover:underline"
        >
          Refresh
        </button>
      </div>

      {loading ? (
        <p className="text-gray-400 text-sm">Loading…</p>
      ) : orders.length === 0 ? (
        <p className="text-gray-400 text-sm py-12 text-center">No orders in queue.</p>
      ) : (
        <div className="space-y-6">
          {Object.entries(byStatus).map(([status, items]) =>
            items.length === 0 ? null : (
              <div key={status}>
                <h2 className="text-sm font-semibold text-gray-500 uppercase tracking-wider mb-3">
                  {status} ({items.length})
                </h2>
                <div className="bg-white border border-gray-200 rounded-xl overflow-hidden">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className="text-left text-xs text-gray-500 border-b border-gray-100 bg-gray-50">
                        <th className="px-5 py-3 font-medium">Accession</th>
                        <th className="px-5 py-3 font-medium">Patient</th>
                        <th className="px-5 py-3 font-medium">Tests</th>
                        <th className="px-5 py-3 font-medium">Priority</th>
                        <th className="px-5 py-3 font-medium">Ordered At</th>
                        <th className="px-5 py-3 font-medium">Action</th>
                      </tr>
                    </thead>
                    <tbody>
                      {items.map((o) => (
                        <tr key={o.labOrderId} className="border-b border-gray-50 hover:bg-gray-50">
                          <td className="px-5 py-3">
                            <Link to={`/lab-orders/${o.labOrderId}`} className="text-sky-600 hover:underline font-mono text-xs">
                              {o.accessionNumber}
                            </Link>
                          </td>
                          <td className="px-5 py-3">
                            <p className="font-medium text-gray-800">{o.patientName}</p>
                            <p className="text-xs text-gray-400 font-mono">{o.mrn}</p>
                          </td>
                          <td className="px-5 py-3 text-gray-600 text-xs">
                            {o.items.map((i) => i.testName).join(', ')}
                          </td>
                          <td className="px-5 py-3">
                            <span className={`px-2 py-0.5 rounded text-xs font-medium ${
                              o.priority === 'STAT' ? 'bg-red-100 text-red-700' :
                              o.priority === 'Urgent' ? 'bg-orange-100 text-orange-700' :
                              'bg-gray-100 text-gray-600'
                            }`}>{o.priority}</span>
                          </td>
                          <td className="px-5 py-3 text-gray-500 text-xs">
                            {new Date(o.createdAt).toLocaleString('en-GB', { dateStyle: 'short', timeStyle: 'short' })}
                          </td>
                          <td className="px-5 py-3">
                            <Link
                              to={`/lab-orders/${o.labOrderId}`}
                              className="text-sky-600 hover:underline text-xs font-medium"
                            >
                              Process →
                            </Link>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>
            ),
          )}
        </div>
      )}
    </div>
  )
}
