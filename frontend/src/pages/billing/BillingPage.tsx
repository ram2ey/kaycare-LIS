import { useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { getBills } from '../../api/billing'
import type { BillSummary } from '../../types/billing'
import { BILL_STATUS_COLORS } from '../../types/billing'

export function BillingPage() {
  const navigate = useNavigate()
  const [bills, setBills] = useState<BillSummary[]>([])
  const [filter, setFilter] = useState<string>('all')
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    getBills().then(setBills).finally(() => setLoading(false))
  }, [])

  const filtered = filter === 'all' ? bills : bills.filter((b) => b.status === filter)
  const totalOutstanding = bills
    .filter((b) => b.status === 'Issued' || b.status === 'PartiallyPaid')
    .reduce((sum, b) => sum + b.balanceDue, 0)

  return (
    <div className="p-6">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Billing</h1>
          {totalOutstanding > 0 && (
            <p className="text-sm text-orange-600 mt-0.5 font-medium">
              ${totalOutstanding.toFixed(2)} outstanding
            </p>
          )}
        </div>
        <Link
          to="/billing/new"
          className="bg-sky-700 hover:bg-sky-800 text-white px-4 py-2 rounded-lg text-sm font-medium transition-colors"
        >
          + New Bill
        </Link>
      </div>

      {/* Status filter */}
      <div className="flex gap-2 mb-4 overflow-x-auto pb-1">
        {['all', 'Draft', 'Issued', 'PartiallyPaid', 'Paid', 'Voided'].map((s) => (
          <button
            key={s}
            onClick={() => setFilter(s)}
            className={`px-3 py-1.5 rounded-lg text-sm font-medium whitespace-nowrap transition-colors ${
              filter === s
                ? 'bg-sky-700 text-white'
                : 'bg-white border border-gray-200 text-gray-600 hover:border-gray-300'
            }`}
          >
            {s === 'all' ? 'All' : s}
          </button>
        ))}
      </div>

      <div className="bg-white border border-gray-200 rounded-xl overflow-hidden">
        {loading ? (
          <p className="py-12 text-center text-gray-400 text-sm">Loading…</p>
        ) : filtered.length === 0 ? (
          <p className="py-12 text-center text-gray-400 text-sm">No bills found.</p>
        ) : (
          <table className="w-full text-sm">
            <thead>
              <tr className="text-left text-xs text-gray-500 border-b border-gray-100 bg-gray-50">
                <th className="px-5 py-3 font-medium">Bill #</th>
                <th className="px-5 py-3 font-medium">Patient</th>
                <th className="px-5 py-3 font-medium">Total</th>
                <th className="px-5 py-3 font-medium">Paid</th>
                <th className="px-5 py-3 font-medium">Balance</th>
                <th className="px-5 py-3 font-medium">Status</th>
                <th className="px-5 py-3 font-medium">Date</th>
              </tr>
            </thead>
            <tbody>
              {filtered.map((b) => (
                <tr
                  key={b.billId}
                  className="border-b border-gray-50 hover:bg-gray-50 cursor-pointer"
                  onClick={() => navigate(`/billing/${b.billId}`)}
                >
                  <td className="px-5 py-3 font-mono text-xs text-sky-700">{b.billNumber}</td>
                  <td className="px-5 py-3">
                    <p className="font-medium text-gray-800">{b.patientName}</p>
                    <p className="text-xs text-gray-400 font-mono">{b.mrn}</p>
                  </td>
                  <td className="px-5 py-3 text-gray-700">${b.totalAmount.toFixed(2)}</td>
                  <td className="px-5 py-3 text-green-700">${b.paidAmount.toFixed(2)}</td>
                  <td className={`px-5 py-3 font-medium ${b.balanceDue > 0 ? 'text-red-600' : 'text-green-600'}`}>
                    ${b.balanceDue.toFixed(2)}
                  </td>
                  <td className="px-5 py-3">
                    <span className={`px-2 py-0.5 rounded text-xs font-medium ${BILL_STATUS_COLORS[b.status] ?? 'bg-gray-100 text-gray-600'}`}>
                      {b.status}
                    </span>
                  </td>
                  <td className="px-5 py-3 text-gray-500 text-xs">
                    {new Date(b.createdAt).toLocaleDateString('en-GB')}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  )
}
