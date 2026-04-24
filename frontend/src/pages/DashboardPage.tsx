import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import { getWaitingList } from '../api/labOrders'
import { getBills } from '../api/billing'

import type { LabOrderSummary } from '../types/labOrders'

export function DashboardPage() {
  const { user } = useAuth()
  const [waiting, setWaiting] = useState<LabOrderSummary[]>([])
  const [outstandingBills, setOutstandingBills] = useState(0)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    Promise.all([
      getWaitingList().then(setWaiting),
      getBills().then((bills) => {
        const outstanding = bills.filter((b) =>
          b.status === 'Issued' || b.status === 'PartiallyPaid',
        ).length
        setOutstandingBills(outstanding)
      }),
    ]).finally(() => setLoading(false))
  }, [])

  return (
    <div className="p-6">
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900">
          Good morning, {user?.firstName}
        </h1>
        <p className="text-gray-500 text-sm mt-1">{new Date().toLocaleDateString('en-GB', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' })}</p>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-3 gap-4 mb-8">
        <StatCard
          label="Waiting for Sample"
          value={loading ? '—' : String(waiting.filter((w) => w.status === 'Ordered').length)}
          color="bg-yellow-50 border-yellow-200 text-yellow-700"
          link="/lab-orders/waiting"
        />
        <StatCard
          label="In Progress"
          value={loading ? '—' : String(waiting.filter((w) => w.status === 'SampleCollected' || w.status === 'InProgress').length)}
          color="bg-blue-50 border-blue-200 text-blue-700"
          link="/lab-orders/waiting"
        />
        <StatCard
          label="Outstanding Bills"
          value={loading ? '—' : String(outstandingBills)}
          color="bg-orange-50 border-orange-200 text-orange-700"
          link="/billing"
        />
      </div>

      {/* Quick actions */}
      <div className="grid grid-cols-4 gap-4 mb-8">
        <QuickAction to="/patients/new" label="Register Patient" icon="👤" />
        <QuickAction to="/lab-orders/new" label="New Lab Order" icon="🧪" />
        <QuickAction to="/billing/new" label="New Bill" icon="🧾" />
        <QuickAction to="/appointments/new" label="Book Appointment" icon="📅" />
      </div>

      {/* Recent waiting */}
      <div className="bg-white border border-gray-200 rounded-xl">
        <div className="px-5 py-4 border-b border-gray-100 flex items-center justify-between">
          <h2 className="font-semibold text-gray-800">Waiting List</h2>
          <Link to="/lab-orders/waiting" className="text-sky-600 text-sm hover:underline">
            View all →
          </Link>
        </div>
        {loading ? (
          <p className="px-5 py-8 text-center text-gray-400 text-sm">Loading…</p>
        ) : waiting.length === 0 ? (
          <p className="px-5 py-8 text-center text-gray-400 text-sm">No orders in queue.</p>
        ) : (
          <table className="w-full text-sm">
            <thead>
              <tr className="text-left text-xs text-gray-500 border-b border-gray-100">
                <th className="px-5 py-3 font-medium">Accession</th>
                <th className="px-5 py-3 font-medium">Patient</th>
                <th className="px-5 py-3 font-medium">Tests</th>
                <th className="px-5 py-3 font-medium">Priority</th>
                <th className="px-5 py-3 font-medium">Status</th>
              </tr>
            </thead>
            <tbody>
              {waiting.slice(0, 8).map((o) => (
                <tr key={o.labOrderId} className="border-b border-gray-50 hover:bg-gray-50">
                  <td className="px-5 py-3">
                    <Link to={`/lab-orders/${o.labOrderId}`} className="text-sky-600 hover:underline font-mono text-xs">
                      {o.accessionNumber}
                    </Link>
                  </td>
                  <td className="px-5 py-3 text-gray-700">{o.patientName}</td>
                  <td className="px-5 py-3 text-gray-500">{o.items.length} test{o.items.length !== 1 ? 's' : ''}</td>
                  <td className="px-5 py-3">
                    <span className={`px-2 py-0.5 rounded text-xs font-medium ${
                      o.priority === 'STAT' ? 'bg-red-100 text-red-700' :
                      o.priority === 'Urgent' ? 'bg-orange-100 text-orange-700' :
                      'bg-gray-100 text-gray-600'
                    }`}>{o.priority}</span>
                  </td>
                  <td className="px-5 py-3">
                    <span className="bg-blue-100 text-blue-700 px-2 py-0.5 rounded text-xs font-medium">
                      {o.status}
                    </span>
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

function StatCard({
  label,
  value,
  color,
  link,
}: {
  label: string
  value: string
  color: string
  link: string
}) {
  return (
    <Link to={link} className={`border rounded-xl p-5 ${color} hover:opacity-90 transition-opacity`}>
      <p className="text-3xl font-bold">{value}</p>
      <p className="text-sm font-medium mt-1 opacity-80">{label}</p>
    </Link>
  )
}

function QuickAction({ to, label, icon }: { to: string; label: string; icon: string }) {
  return (
    <Link
      to={to}
      className="bg-white border border-gray-200 rounded-xl p-4 text-center hover:border-sky-300 hover:bg-sky-50 transition-colors"
    >
      <p className="text-2xl mb-1">{icon}</p>
      <p className="text-sm font-medium text-gray-700">{label}</p>
    </Link>
  )
}
