import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import { getWaitingList } from '../api/labOrders'
import { getRadiologyWorklist, getRadiologyStats } from '../api/radiology'
import { getBills } from '../api/billing'
import { getFacilitySettings } from '../api/facility'
import { CriticalAlertsWidget } from '../components/CriticalAlertsWidget'

import type { LabOrderSummary } from '../types/labOrders'
import type { RadiologyOrderSummary, RadiologyStatsResponse } from '../types/radiology'
import type { FacilitySettingsResponse } from '../types/facility'

export function DashboardPage() {
  const { user } = useAuth()
  const [settings, setSettings] = useState<FacilitySettingsResponse | null>(null)
  const [waiting, setWaiting] = useState<LabOrderSummary[]>([])
  const [radiologyOrders, setRadiologyOrders] = useState<RadiologyOrderSummary[]>([])
  const [radStats, setRadStats] = useState<RadiologyStatsResponse | null>(null)
  const [outstandingBills, setOutstandingBills] = useState(0)
  const [loading, setLoading] = useState(true)
  const [activeTab, setActiveTab] = useState<'all' | 'lab' | 'rad'>('all')

  const isAdmin = user?.role === 'SuperAdmin' || user?.role === 'Admin'
  const isLabEnabled = settings?.isLaboratoryEnabled ?? true
  const isRadEnabled = settings?.isRadiologyEnabled ?? true
  const isUserLab = user?.department === 'Laboratory'
  const isUserRad = user?.department === 'Radiology'

  const showLab = isLabEnabled && (isAdmin || !isUserRad)
  const showRad = isRadEnabled && (isAdmin || !isUserLab)

  useEffect(() => {
    getFacilitySettings()
      .then((settingsRes) => {
        setSettings(settingsRes)
        const currentLabEnabled = settingsRes.isLaboratoryEnabled && (isAdmin || !isUserRad)
        const currentRadEnabled = settingsRes.isRadiologyEnabled && (isAdmin || !isUserLab)

        const promises: Promise<any>[] = []
        if (currentLabEnabled) {
          promises.push(getWaitingList().then(setWaiting).catch(() => {}))
        }
        if (currentRadEnabled) {
          promises.push(getRadiologyWorklist().then(setRadiologyOrders).catch(() => {}))
          promises.push(getRadiologyStats().then(setRadStats).catch(() => {}))
        }
        promises.push(getBills().then((bills) => {
          const outstanding = bills.filter((b) =>
            b.status === 'Issued' || b.status === 'PartiallyPaid',
          ).length
          setOutstandingBills(outstanding)
        }).catch(() => {}))

        return Promise.all(promises)
      })
      .catch((err) => {
        console.error('Failed to load settings', err)
        return Promise.all([
          getWaitingList().then(setWaiting).catch(() => {}),
          getRadiologyWorklist().then(setRadiologyOrders).catch(() => {}),
          getRadiologyStats().then(setRadStats).catch(() => {}),
          getBills().then((bills) => {
            const outstanding = bills.filter((b) =>
              b.status === 'Issued' || b.status === 'PartiallyPaid',
            ).length
            setOutstandingBills(outstanding)
          }).catch(() => {}),
        ])
      })
      .finally(() => setLoading(false))
  }, [user, isAdmin, isUserLab, isUserRad])

  useEffect(() => {
    if (showLab && showRad) {
      setActiveTab('all')
    } else if (showLab) {
      setActiveTab('lab')
    } else if (showRad) {
      setActiveTab('rad')
    }
  }, [showLab, showRad])

  // Diagnostics calculations
  const labWaiting = waiting.filter((w) => w.status === 'Ordered').length
  const labInProgress = waiting.filter((w) => w.status === 'SampleCollected' || w.status === 'InProgress' || w.status === 'PartiallyCompleted').length

  // Mock chart data representing recent daily diagnostic runs
  const weeklyTrends = [
    { day: 'Mon', lab: 24, rad: 8 },
    { day: 'Tue', lab: 38, rad: 14 },
    { day: 'Wed', lab: 30, rad: 11 },
    { day: 'Thu', lab: 45, rad: 17 },
    { day: 'Fri', lab: 35, rad: 15 },
    { day: 'Sat', lab: 18, rad: 6 },
    { day: 'Sun', lab: 12, rad: 4 },
  ]

  // SVG chart scaling helper functions
  const maxVal = 50
  const width = 600
  const height = 140
  const padding = 25

  const getCoordinates = (index: number, value: number) => {
    const x = padding + (index * (width - padding * 2)) / (weeklyTrends.length - 1)
    const y = height - padding - (value * (height - padding * 2)) / maxVal
    return { x, y }
  }

  // Generate SVG path strings
  const labPath = weeklyTrends.map((d, i) => getCoordinates(i, d.lab))
  const radPath = weeklyTrends.map((d, i) => getCoordinates(i, d.rad))

  const createPathString = (points: { x: number; y: number }[]) =>
    `M ${points[0].x} ${points[0].y} ` + points.slice(1).map((p) => `L ${p.x} ${p.y}`).join(' ')

  const createAreaPathString = (points: { x: number; y: number }[]) => {
    const mainPath = createPathString(points)
    return `${mainPath} L ${points[points.length - 1].x} ${height - padding} L ${points[0].x} ${height - padding} Z`
  }

  return (
    <div className="p-6 max-w-7xl mx-auto space-y-6">
      {/* Header card with gradient border */}
      <div className="relative overflow-hidden bg-gradient-to-r from-sky-900 to-indigo-955 p-6 rounded-2xl text-white shadow-lg border border-sky-850">
        <div className="absolute right-0 top-0 w-64 h-64 bg-white/5 rounded-full blur-3xl -mr-20 -mt-20 pointer-events-none"></div>
        <div className="absolute left-1/3 bottom-0 w-48 h-48 bg-sky-500/10 rounded-full blur-2xl pointer-events-none"></div>
        
        <div className="relative z-10 flex flex-col md:flex-row md:items-center md:justify-between gap-4">
          <div>
            <h1 className="text-3xl font-extrabold tracking-tight">
              Good morning, {user?.firstName}
            </h1>
            <p className="text-sky-200 text-sm mt-1.5 flex items-center gap-1.5 font-medium">
              <span>📅</span>
              {new Date().toLocaleDateString('en-GB', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' })}
            </p>
          </div>
          <div className="flex gap-2">
            <Link to="/patients/new" className="bg-sky-500 hover:bg-sky-400 text-white font-semibold text-xs px-4 py-2.5 rounded-xl shadow-md transition-transform hover:scale-[1.02] flex items-center gap-1.5">
              <span>👤</span> Register Patient
            </Link>
            {showLab && (
              <Link to="/lab-orders/new" className="bg-white/10 hover:bg-white/15 text-white font-semibold text-xs px-4 py-2.5 rounded-xl transition-transform hover:scale-[1.02] flex items-center gap-1.5 border border-white/10 backdrop-blur-sm">
                <span>🧪</span> Lab Order
              </Link>
            )}
            {showRad && (
              <Link to="/radiology/new" className="bg-white/10 hover:bg-white/15 text-white font-semibold text-xs px-4 py-2.5 rounded-xl transition-transform hover:scale-[1.02] flex items-center gap-1.5 border border-white/10 backdrop-blur-sm">
                <span>🩻</span> Radiology Order
              </Link>
            )}
          </div>
        </div>
      </div>

      {showLab && (
        <CriticalAlertsWidget onAlertResolved={() => {
          getWaitingList().then(setWaiting).catch(() => {})
        }} />
      )}

      {/* Analytics grid */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Statistics cards */}
        <div className={`lg:col-span-2 grid gap-4 ${
          showLab && showRad ? 'grid-cols-2 sm:grid-cols-5' :
          showLab ? 'grid-cols-2' :
          showRad ? 'grid-cols-3' : 'grid-cols-1'
        }`}>
          {showLab && (
            <>
              <StatCard
                label="Lab: Pending Sample"
                value={loading ? '—' : String(labWaiting)}
                color="border-amber-250 bg-gradient-to-br from-amber-50 to-orange-50 text-amber-900"
                subtitle="Ordered tests"
                icon="🧪"
                link="/lab-orders/waiting"
              />
              <StatCard
                label="Lab: In Progress"
                value={loading ? '—' : String(labInProgress)}
                color="border-sky-250 bg-gradient-to-br from-sky-50 to-blue-50 text-sky-900"
                subtitle="Processing samples"
                icon="⚙️"
                link="/lab-orders/waiting"
              />
            </>
          )}
          {showRad && (
            <>
              <StatCard
                label="Rad: Scheduled"
                value={loading ? '—' : String(radStats?.scheduledCount ?? 0)}
                color="border-indigo-250 bg-gradient-to-br from-indigo-50 to-violet-50 text-indigo-900"
                subtitle="Scans queued"
                icon="🩻"
                link="/radiology"
              />
              <StatCard
                label="Rad: Acquired"
                value={loading ? '—' : String(radStats?.acquiredCount ?? 0)}
                color="border-teal-250 bg-gradient-to-br from-teal-50 to-emerald-50 text-teal-900"
                subtitle="Scans completed"
                icon="⚡"
                link="/radiology"
              />
              <StatCard
                label="Rad: Reported"
                value={loading ? '—' : String(radStats?.reportedCount ?? 0)}
                color="border-pink-250 bg-gradient-to-br from-pink-50 to-fuchsia-50 text-pink-900"
                subtitle="Reports pending"
                icon="✍️"
                link="/radiology"
              />
            </>
          )}
        </div>

        {/* Financial alert card */}
        <Link to="/billing" className="border border-red-200 bg-gradient-to-br from-red-50 to-rose-50 rounded-2xl p-5 hover:scale-[1.01] hover:shadow-md transition-all flex flex-col justify-between group">
          <div className="flex justify-between items-start">
            <div>
              <p className="text-red-950 font-bold text-lg">Billing Health</p>
              <p className="text-red-600 text-xs mt-0.5">Outstanding invoices</p>
            </div>
            <span className="text-2xl opacity-75 group-hover:scale-110 transition-transform">🧾</span>
          </div>
          <div className="mt-4 flex items-baseline gap-2">
            <span className="text-4xl font-extrabold text-red-950">{loading ? '—' : outstandingBills}</span>
            <span className="text-xs font-semibold text-red-700 bg-red-100 px-2 py-0.5 rounded-full">Requires attention</span>
          </div>
        </Link>
      </div>

      {/* SVG chart section */}
      {(showLab || showRad) && (
        <div className="bg-white border border-gray-150 rounded-2xl p-5 shadow-sm">
          <div className="flex flex-col sm:flex-row sm:items-center justify-between mb-4 gap-2">
            <div>
              <h2 className="font-bold text-gray-800 text-base">Weekly Diagnostic Activity</h2>
              <p className="text-gray-400 text-xs mt-0.5">Comparative load trends for LIS & RIS</p>
            </div>
            <div className="flex items-center gap-4 text-xs font-semibold">
              {showLab && (
                <div className="flex items-center gap-1.5 text-sky-600">
                  <span className="w-2.5 h-2.5 bg-sky-500 rounded-full"></span>
                  <span>Laboratory ({weeklyTrends.reduce((sum, item) => sum + item.lab, 0)})</span>
                </div>
              )}
              {showRad && (
                <div className="flex items-center gap-1.5 text-violet-600">
                  <span className="w-2.5 h-2.5 bg-violet-500 rounded-full"></span>
                  <span>Radiology ({weeklyTrends.reduce((sum, item) => sum + item.rad, 0)})</span>
                </div>
              )}
            </div>
          </div>

          {/* Interactive SVG Chart */}
          <div className="relative w-full h-[150px] overflow-hidden">
            <svg className="w-full h-full" viewBox={`0 0 ${width} ${height}`} preserveAspectRatio="none">
              <defs>
                <linearGradient id="labGrad" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="0%" stopColor="#0ea5e9" stopOpacity="0.25" />
                  <stop offset="100%" stopColor="#0ea5e9" stopOpacity="0.0" />
                </linearGradient>
                <linearGradient id="radGrad" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="0%" stopColor="#8b5cf6" stopOpacity="0.25" />
                  <stop offset="100%" stopColor="#8b5cf6" stopOpacity="0.0" />
                </linearGradient>
              </defs>

              {/* Gridlines */}
              <line x1={padding} y1={padding} x2={width - padding} y2={padding} stroke="#f1f5f9" strokeWidth="1" />
              <line x1={padding} y1={(height - padding * 2) / 2 + padding} x2={width - padding} y2={(height - padding * 2) / 2 + padding} stroke="#f1f5f9" strokeWidth="1" />
              <line x1={padding} y1={height - padding} x2={width - padding} y2={height - padding} stroke="#e2e8f0" strokeWidth="1.5" />

              {/* Area gradients */}
              {showLab && labPath.length > 0 && <path d={createAreaPathString(labPath)} fill="url(#labGrad)" />}
              {showRad && radPath.length > 0 && <path d={createAreaPathString(radPath)} fill="url(#radGrad)" />}

              {/* Line paths */}
              {showLab && labPath.length > 0 && <path d={createPathString(labPath)} fill="none" stroke="#0ea5e9" strokeWidth="2.5" strokeLinecap="round" />}
              {showRad && radPath.length > 0 && <path d={createPathString(radPath)} fill="none" stroke="#8b5cf6" strokeWidth="2.5" strokeLinecap="round" />}

              {/* Path node dots */}
              {showLab && labPath.map((p, i) => (
                <circle key={`l-${i}`} cx={p.x} cy={p.y} r="3.5" fill="#ffffff" stroke="#0ea5e9" strokeWidth="2" className="transition-all hover:r-5 cursor-pointer" />
              ))}
              {showRad && radPath.map((p, i) => (
                <circle key={`r-${i}`} cx={p.x} cy={p.y} r="3.5" fill="#ffffff" stroke="#8b5cf6" strokeWidth="2" className="transition-all hover:r-5 cursor-pointer" />
              ))}

              {/* X-Axis labels */}
              {weeklyTrends.map((d, i) => {
                const x = padding + (i * (width - padding * 2)) / (weeklyTrends.length - 1)
                return (
                  <text key={`lbl-${i}`} x={x} y={height - 8} textAnchor="middle" fill="#94a3b8" fontSize="9" fontWeight="bold" fontFamily="sans-serif">
                    {d.day}
                  </text>
                )
              })}
            </svg>
          </div>
        </div>
      )}

      {/* Split Queues Section */}
      <div className="bg-white border border-gray-150 rounded-2xl shadow-sm overflow-hidden">
        {/* Header Tabs */}
        <div className="px-5 py-4 border-b border-gray-100 flex items-center justify-between flex-wrap gap-4 bg-gray-50/50">
          <div>
            <h2 className="font-bold text-gray-800 text-base">Active Diagnostics Worklist</h2>
            <p className="text-gray-400 text-xs mt-0.5">Orders requiring action today</p>
          </div>
          {showLab && showRad && (
            <div className="flex border border-gray-200 rounded-lg p-0.5 bg-white text-xs font-semibold shadow-sm">
              <button
                onClick={() => setActiveTab('all')}
                className={`px-3 py-1.5 rounded-md transition-colors ${activeTab === 'all' ? 'bg-sky-700 text-white' : 'text-gray-500 hover:text-gray-800'}`}
              >
                All Queue ({waiting.length + radiologyOrders.length})
              </button>
              <button
                onClick={() => setActiveTab('lab')}
                className={`px-3 py-1.5 rounded-md transition-colors ${activeTab === 'lab' ? 'bg-sky-750 text-white' : 'text-gray-500 hover:text-gray-850'}`}
              >
                Lab ({waiting.length})
              </button>
              <button
                onClick={() => setActiveTab('rad')}
                className={`px-3 py-1.5 rounded-md transition-colors ${activeTab === 'rad' ? 'bg-indigo-700 text-white' : 'text-gray-500 hover:text-gray-800'}`}
              >
                Radiology ({radiologyOrders.length})
              </button>
            </div>
          )}
        </div>

        {/* Tab contents */}
        {loading ? (
          <p className="p-12 text-center text-gray-400 text-sm">Loading worklists…</p>
        ) : (
          <div className="divide-y divide-gray-100">
            {/* Laboratory List */}
            {showLab && (activeTab === 'all' || activeTab === 'lab') && (
              <div className="p-5">
                <div className="flex items-center justify-between mb-3">
                  <h3 className="text-xs font-bold text-sky-750 uppercase tracking-widest flex items-center gap-1.5">
                    <span>🧪</span> Clinical Lab Orders ({waiting.length})
                  </h3>
                  <Link to="/lab-orders/waiting" className="text-xs text-sky-650 hover:underline font-semibold">
                    Open Lab Waiting List →
                  </Link>
                </div>

                {waiting.length === 0 ? (
                  <p className="text-xs text-gray-400 py-6 text-center">No lab orders in queue.</p>
                ) : (
                  <div className="overflow-x-auto">
                    <table className="w-full text-left text-xs border-collapse">
                      <thead>
                        <tr className="text-gray-500 border-b border-gray-100 font-semibold bg-gray-50/50">
                          <th className="py-2.5 px-3">Accession</th>
                          <th className="py-2.5 px-3">Patient</th>
                          <th className="py-2.5 px-3">Tests Ordered</th>
                          <th className="py-2.5 px-3">Priority</th>
                          <th className="py-2.5 px-3">Status</th>
                        </tr>
                      </thead>
                      <tbody className="divide-y divide-gray-50">
                        {waiting.slice(0, 5).map((o) => (
                          <tr key={o.labOrderId} className="hover:bg-sky-50/30 transition-colors">
                            <td className="py-2.5 px-3 font-mono text-sky-750 font-bold">
                              <Link to={`/lab-orders/${o.labOrderId}`} className="hover:underline">
                                {o.accessionNumber ?? 'N/A'}
                              </Link>
                            </td>
                            <td className="py-2.5 px-3">
                              <span className="font-bold text-gray-800">{o.patientName}</span>
                              <span className="text-gray-400 font-mono text-[10px] ml-1.5">{o.patientMrn}</span>
                            </td>
                            <td className="py-2.5 px-3 text-gray-500 truncate max-w-xs">{o.testNames.join(', ')}</td>
                            <td className="py-2.5 px-3">
                              <span className={`inline-flex items-center gap-1.5 px-2 py-0.5 rounded-full text-[10px] font-bold ${
                                o.priority === 'STAT' ? 'bg-red-100 text-red-700 border border-red-200' :
                                o.priority === 'Urgent' ? 'bg-orange-100 text-orange-700' :
                                'bg-gray-150 text-gray-600'
                              }`}>
                                {o.priority === 'STAT' && (
                                  <span className="relative flex h-1.5 w-1.5">
                                    <span className="absolute inline-flex h-full w-full rounded-full bg-red-400 opacity-75 animate-ping"></span>
                                    <span className="relative inline-flex rounded-full h-1.5 w-1.5 bg-red-500"></span>
                                  </span>
                                )}
                                {o.priority}
                              </span>
                            </td>
                            <td className="py-2.5 px-3">
                              <span className="bg-sky-100 text-sky-850 px-2 py-0.5 rounded text-[10px] font-bold">
                                {o.status}
                              </span>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}
              </div>
            )}

            {/* Radiology List */}
            {showRad && (activeTab === 'all' || activeTab === 'rad') && (
              <div className="p-5 bg-indigo-50/5">
                <div className="flex items-center justify-between mb-3">
                  <h3 className="text-xs font-bold text-indigo-800 uppercase tracking-widest flex items-center gap-1.5">
                    <span>🩻</span> Radiology Studies ({radiologyOrders.length})
                  </h3>
                  <Link to="/radiology" className="text-xs text-indigo-750 hover:underline font-semibold">
                    Open Radiology Worklist →
                  </Link>
                </div>

                {radiologyOrders.length === 0 ? (
                  <p className="text-xs text-gray-400 py-6 text-center">No radiology orders scheduled.</p>
                ) : (
                  <div className="overflow-x-auto">
                    <table className="w-full text-left text-xs border-collapse">
                      <thead>
                        <tr className="text-gray-500 border-b border-gray-100 font-semibold bg-gray-50/50">
                          <th className="py-2.5 px-3">Accession / Study</th>
                          <th className="py-2.5 px-3">Patient</th>
                          <th className="py-2.5 px-3">Procedures</th>
                          <th className="py-2.5 px-3">Priority</th>
                          <th className="py-2.5 px-3">Status</th>
                        </tr>
                      </thead>
                      <tbody className="divide-y divide-gray-50">
                        {radiologyOrders.slice(0, 5).map((o) => (
                          <tr key={o.radiologyOrderId} className="hover:bg-indigo-50/30 transition-colors">
                            <td className="py-2.5 px-3 font-mono text-indigo-800 font-bold">
                              <Link to={`/radiology/${o.radiologyOrderId}`} className="hover:underline">
                                RAD-{(o.radiologyOrderId.substring(0, 4).toUpperCase())}
                              </Link>
                            </td>
                            <td className="py-2.5 px-3">
                              <span className="font-bold text-gray-800">{o.patientName}</span>
                              <span className="text-gray-400 font-mono text-[10px] ml-1.5">{o.patientMrn}</span>
                            </td>
                            <td className="py-2.5 px-3 text-gray-500 truncate max-w-xs">{o.procedureNames.join(', ')}</td>
                            <td className="py-2.5 px-3">
                              <span className={`inline-flex items-center gap-1.5 px-2 py-0.5 rounded-full text-[10px] font-bold ${
                                o.priority === 'STAT' ? 'bg-red-100 text-red-700 border border-red-200' :
                                o.priority === 'Urgent' ? 'bg-orange-100 text-orange-700' :
                                'bg-gray-150 text-gray-600'
                              }`}>
                                {o.priority === 'STAT' && (
                                  <span className="relative flex h-1.5 w-1.5">
                                    <span className="absolute inline-flex h-full w-full rounded-full bg-red-400 opacity-75 animate-ping"></span>
                                    <span className="relative inline-flex rounded-full h-1.5 w-1.5 bg-red-500"></span>
                                  </span>
                                )}
                                {o.priority}
                              </span>
                            </td>
                            <td className="py-2.5 px-3">
                              <span className="bg-indigo-100 text-indigo-800 px-2 py-0.5 rounded text-[10px] font-bold">
                                {o.status}
                              </span>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  )
}

function StatCard({
  label,
  value,
  color,
  subtitle,
  icon,
  link,
}: {
  label: string
  value: string
  color: string
  subtitle: string
  icon: string
  link: string
}) {
  return (
    <Link to={link} className={`border rounded-2xl p-4 flex flex-col justify-between hover:scale-[1.02] hover:shadow-md transition-all group ${color}`}>
      <div className="flex justify-between items-start">
        <span className="text-[10px] font-bold uppercase tracking-wider opacity-75">{label}</span>
        <span className="text-lg opacity-75 group-hover:scale-110 transition-transform">{icon}</span>
      </div>
      <div className="mt-4">
        <p className="text-3xl font-extrabold">{value}</p>
        <p className="text-[10px] font-semibold opacity-65 mt-0.5">{subtitle}</p>
      </div>
    </Link>
  )
}
