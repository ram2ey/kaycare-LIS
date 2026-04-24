import { useEffect, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import {
  getLabOrder,
  receiveSample,
  enterManualResult,
  signItem,
  downloadLabReport,
} from '../../api/labOrders'
import type { LabOrderDetail, LabOrderItemResponse } from '../../types/labOrders'
import { ORDER_STATUS_COLORS, ITEM_STATUS_COLORS, FLAG_COLORS } from '../../types/labOrders'
import { useAuth } from '../../context/AuthContext'

export function LabOrderDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { user } = useAuth()
  const [order, setOrder] = useState<LabOrderDetail | null>(null)
  const [loading, setLoading] = useState(true)
  const [resultItem, setResultItem] = useState<LabOrderItemResponse | null>(null)
  const [resultForm, setResultForm] = useState({ value: '', unit: '', referenceRange: '', notes: '' })
  const [submitting, setSubmitting] = useState(false)

  function load() {
    if (!id) return
    setLoading(true)
    getLabOrder(id).then(setOrder).finally(() => setLoading(false))
  }

  useEffect(() => { load() }, [id])

  async function handleReceiveSample() {
    if (!id || !confirm('Mark sample as received?')) return
    await receiveSample(id)
    load()
  }

  async function handleEnterResult(e: React.FormEvent) {
    e.preventDefault()
    if (!id || !resultItem) return
    setSubmitting(true)
    try {
      await enterManualResult(id, resultItem.labOrderItemId, {
        value: resultForm.value,
        unit: resultForm.unit || undefined,
        referenceRange: resultForm.referenceRange || undefined,
        notes: resultForm.notes || undefined,
      })
      setResultItem(null)
      setResultForm({ value: '', unit: '', referenceRange: '', notes: '' })
      load()
    } finally {
      setSubmitting(false)
    }
  }

  async function handleSign(itemId: string) {
    if (!id || !confirm('Sign off this result?')) return
    await signItem(id, itemId)
    load()
  }

  async function handleDownloadReport() {
    if (!id) return
    const blob = await downloadLabReport(id)
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `lab-report-${order?.accessionNumber}.pdf`
    a.click()
    URL.revokeObjectURL(url)
  }

  const canResult = ['LabTechnician', 'Doctor', 'Admin', 'SuperAdmin'].includes(user?.role ?? '')
  const canSign = ['Doctor', 'Admin', 'SuperAdmin'].includes(user?.role ?? '')
  const canReceive = ['LabTechnician', 'Nurse', 'Doctor', 'Admin', 'SuperAdmin'].includes(user?.role ?? '')

  if (loading) return <div className="p-6 text-gray-400 text-sm">Loading…</div>
  if (!order) return <div className="p-6 text-red-600 text-sm">Order not found.</div>

  const isCompleted = order.status === 'Completed'
  const canReceiveSample = order.status === 'Ordered' && canReceive

  return (
    <div className="p-6 max-w-4xl">
      <button onClick={() => navigate(-1)} className="text-sm text-gray-500 hover:text-gray-700 mb-4">← Back</button>

      {/* Header */}
      <div className="flex items-start justify-between mb-6">
        <div>
          <div className="flex items-center gap-3 mb-1">
            <h1 className="text-2xl font-bold text-gray-900 font-mono">{order.accessionNumber}</h1>
            <span className={`px-2.5 py-1 rounded text-xs font-medium ${ORDER_STATUS_COLORS[order.status] ?? 'bg-gray-100 text-gray-600'}`}>
              {order.status}
            </span>
            <span className={`px-2 py-0.5 rounded text-xs font-medium ${
              order.priority === 'STAT' ? 'bg-red-100 text-red-700' :
              order.priority === 'Urgent' ? 'bg-orange-100 text-orange-700' :
              'bg-gray-100 text-gray-600'
            }`}>{order.priority}</span>
          </div>
          <p className="text-gray-600 text-sm">
            {order.patientName} · <span className="font-mono text-xs">{order.mrn}</span>
          </p>
          <p className="text-gray-400 text-xs mt-0.5">Ordered by {order.orderingDoctorName} · {new Date(order.createdAt).toLocaleString('en-GB')}</p>
        </div>
        <div className="flex gap-2">
          {canReceiveSample && (
            <button
              onClick={handleReceiveSample}
              className="bg-yellow-500 hover:bg-yellow-600 text-white px-3 py-1.5 rounded-lg text-sm font-medium"
            >
              Receive Sample
            </button>
          )}
          {isCompleted && (
            <button
              onClick={handleDownloadReport}
              className="bg-sky-700 hover:bg-sky-800 text-white px-3 py-1.5 rounded-lg text-sm font-medium"
            >
              Download Report
            </button>
          )}
        </div>
      </div>

      {order.clinicalNotes && (
        <div className="bg-yellow-50 border border-yellow-200 rounded-xl px-4 py-3 mb-6 text-sm text-yellow-800">
          <span className="font-medium">Clinical Notes: </span>{order.clinicalNotes}
        </div>
      )}

      {/* Results table */}
      <div className="bg-white border border-gray-200 rounded-xl overflow-hidden mb-6">
        <div className="px-5 py-4 border-b border-gray-100">
          <h2 className="font-semibold text-gray-800">Test Results</h2>
        </div>
        <table className="w-full text-sm">
          <thead>
            <tr className="text-left text-xs text-gray-500 border-b border-gray-100 bg-gray-50">
              <th className="px-5 py-3 font-medium">Test</th>
              <th className="px-5 py-3 font-medium">Department</th>
              <th className="px-5 py-3 font-medium">Result</th>
              <th className="px-5 py-3 font-medium">Reference Range</th>
              <th className="px-5 py-3 font-medium">Flag</th>
              <th className="px-5 py-3 font-medium">Status</th>
              <th className="px-5 py-3 font-medium">Actions</th>
            </tr>
          </thead>
          <tbody>
            {order.items.map((item) => {
              const result = item.manualResultValue ?? item.hl7ResultValue
              const unit = item.manualResultUnit ?? item.hl7ResultUnit
              const refRange = item.manualResultReferenceRange
              const flag = item.manualResultFlag ?? item.hl7Flag
              const canEnterResult = canResult && (item.status === 'SampleCollected' || item.status === 'Pending')
              const canSignOff = canSign && item.status === 'Resulted'

              return (
                <tr key={item.labOrderItemId} className="border-b border-gray-50">
                  <td className="px-5 py-3">
                    <p className="font-medium text-gray-800">{item.testName}</p>
                    <p className="text-xs text-gray-400 font-mono">{item.testCode}</p>
                  </td>
                  <td className="px-5 py-3 text-gray-500 text-xs">{item.department}</td>
                  <td className="px-5 py-3">
                    {result ? (
                      <span className="font-medium text-gray-800">{result} {unit}</span>
                    ) : (
                      <span className="text-gray-300">—</span>
                    )}
                  </td>
                  <td className="px-5 py-3 text-gray-500 text-xs">{refRange ?? '—'}</td>
                  <td className="px-5 py-3">
                    {flag ? (
                      <span className={`font-bold text-sm ${FLAG_COLORS[flag] ?? ''}`}>{flag}</span>
                    ) : (
                      <span className="text-gray-300">—</span>
                    )}
                  </td>
                  <td className="px-5 py-3">
                    <span className={`px-2 py-0.5 rounded text-xs font-medium ${ITEM_STATUS_COLORS[item.status] ?? 'bg-gray-100 text-gray-600'}`}>
                      {item.status}
                    </span>
                  </td>
                  <td className="px-5 py-3">
                    {canEnterResult && (
                      <button
                        onClick={() => {
                          setResultItem(item)
                          setResultForm({
                            value: '',
                            unit: item.manualResultUnit ?? '',
                            referenceRange: item.manualResultReferenceRange ?? '',
                            notes: '',
                          })
                        }}
                        className="text-sky-600 hover:underline text-xs font-medium"
                      >
                        Enter Result
                      </button>
                    )}
                    {canSignOff && (
                      <button
                        onClick={() => handleSign(item.labOrderItemId)}
                        className="text-green-600 hover:underline text-xs font-medium ml-2"
                      >
                        Sign
                      </button>
                    )}
                    {item.signedAt && (
                      <span className="text-gray-400 text-xs">
                        Signed {new Date(item.signedAt).toLocaleDateString('en-GB')}
                      </span>
                    )}
                  </td>
                </tr>
              )
            })}
          </tbody>
        </table>
      </div>

      {/* Enter result modal */}
      {resultItem && (
        <div className="fixed inset-0 bg-black/30 flex items-center justify-center z-50">
          <div className="bg-white rounded-2xl shadow-xl p-6 w-full max-w-sm">
            <h3 className="font-semibold text-gray-800 mb-4">Enter Result — {resultItem.testName}</h3>
            <form onSubmit={handleEnterResult} className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Result Value <span className="text-red-500">*</span></label>
                <input
                  required
                  value={resultForm.value}
                  onChange={(e) => setResultForm({ ...resultForm, value: e.target.value })}
                  placeholder="e.g. 5.2"
                  className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Unit</label>
                <input
                  value={resultForm.unit}
                  onChange={(e) => setResultForm({ ...resultForm, unit: e.target.value })}
                  placeholder="e.g. mmol/L"
                  className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Reference Range</label>
                <input
                  value={resultForm.referenceRange}
                  onChange={(e) => setResultForm({ ...resultForm, referenceRange: e.target.value })}
                  placeholder="e.g. 3.9-5.6"
                  className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Notes</label>
                <textarea
                  rows={2}
                  value={resultForm.notes}
                  onChange={(e) => setResultForm({ ...resultForm, notes: e.target.value })}
                  className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
                />
              </div>
              <div className="flex gap-3 pt-2">
                <button
                  type="button"
                  onClick={() => setResultItem(null)}
                  className="flex-1 border border-gray-300 rounded-lg py-2 text-sm hover:bg-gray-50"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={submitting}
                  className="flex-1 bg-sky-700 text-white rounded-lg py-2 text-sm font-medium hover:bg-sky-800 disabled:opacity-60"
                >
                  {submitting ? 'Saving…' : 'Save Result'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  )
}
