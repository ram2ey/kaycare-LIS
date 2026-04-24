import { useState, useEffect } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { createBill } from '../../api/billing'
import { searchPatients, getPatient } from '../../api/patients'

interface LineItem {
  description: string
  category: string
  quantity: number
  unitPrice: number
}

export function NewBillPage() {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const patientIdParam = searchParams.get('patientId') ?? ''

  const [patientId, setPatientId] = useState(patientIdParam)
  const [patientName, setPatientName] = useState('')
  const [patientQuery, setPatientQuery] = useState('')
  const [suggestions, setSuggestions] = useState<{ patientId: string; name: string; mrn: string }[]>([])
  const [items, setItems] = useState<LineItem[]>([{ description: '', category: '', quantity: 1, unitPrice: 0 }])
  const [notes, setNotes] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    if (patientIdParam) {
      getPatient(patientIdParam).then((p) => {
        setPatientName(`${p.firstName} ${p.lastName} (${p.mrn})`)
      })
    }
  }, [patientIdParam])

  async function handlePatientSearch(q: string) {
    setPatientQuery(q)
    if (q.length < 2) { setSuggestions([]); return }
    const res = await searchPatients(q, 1, 6)
    setSuggestions(res.items.map((p) => ({ patientId: p.patientId, name: `${p.firstName} ${p.lastName}`, mrn: p.mrn })))
  }

  function addItem() {
    setItems((i) => [...i, { description: '', category: '', quantity: 1, unitPrice: 0 }])
  }

  function updateItem(idx: number, field: keyof LineItem, value: string | number) {
    setItems((items) => items.map((item, i) => i === idx ? { ...item, [field]: value } : item))
  }

  function removeItem(idx: number) {
    setItems((items) => items.filter((_, i) => i !== idx))
  }

  const total = items.reduce((sum, item) => sum + item.quantity * item.unitPrice, 0)

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!patientId) { setError('Please select a patient.'); return }
    if (items.every((i) => !i.description)) { setError('Add at least one item.'); return }
    setError('')
    setLoading(true)
    try {
      const bill = await createBill({
        patientId,
        notes: notes || undefined,
        items: items.filter((i) => i.description).map((i) => ({
          description: i.description,
          category: i.category || undefined,
          quantity: i.quantity,
          unitPrice: i.unitPrice,
        })),
      })
      navigate(`/billing/${bill.billId}`)
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message
      setError(msg ?? 'Failed to create bill.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="p-6 max-w-3xl">
      <div className="mb-6">
        <button onClick={() => navigate(-1)} className="text-sm text-gray-500 hover:text-gray-700 mb-2">← Back</button>
        <h1 className="text-2xl font-bold text-gray-900">New Bill</h1>
      </div>

      <form onSubmit={handleSubmit} className="space-y-6">
        {/* Patient */}
        <div className="bg-white border border-gray-200 rounded-xl p-5">
          <h2 className="font-semibold text-gray-700 mb-3">Patient</h2>
          {patientId && patientName ? (
            <div className="flex items-center justify-between">
              <p className="text-sm font-medium text-gray-800">{patientName}</p>
              <button type="button" onClick={() => { setPatientId(''); setPatientName(''); setPatientQuery('') }}
                className="text-xs text-red-500 hover:underline">Change</button>
            </div>
          ) : (
            <div className="relative">
              <input
                type="text"
                placeholder="Search patient…"
                value={patientQuery}
                onChange={(e) => handlePatientSearch(e.target.value)}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
              />
              {suggestions.length > 0 && (
                <div className="absolute top-full mt-1 w-full bg-white border border-gray-200 rounded-lg shadow-lg z-10">
                  {suggestions.map((s) => (
                    <button key={s.patientId} type="button"
                      onClick={() => { setPatientId(s.patientId); setPatientName(`${s.name} (${s.mrn})`); setPatientQuery(`${s.name} (${s.mrn})`); setSuggestions([]) }}
                      className="w-full text-left px-3 py-2 text-sm hover:bg-gray-50 border-b border-gray-50 last:border-0"
                    >
                      <span className="font-medium">{s.name}</span>{' '}
                      <span className="text-gray-400 font-mono text-xs">{s.mrn}</span>
                    </button>
                  ))}
                </div>
              )}
            </div>
          )}
        </div>

        {/* Line items */}
        <div className="bg-white border border-gray-200 rounded-xl p-5">
          <h2 className="font-semibold text-gray-700 mb-4">Line Items</h2>
          <div className="space-y-3">
            {items.map((item, idx) => (
              <div key={idx} className="grid grid-cols-12 gap-2 items-center">
                <input
                  className="col-span-4 border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
                  placeholder="Description"
                  value={item.description}
                  onChange={(e) => updateItem(idx, 'description', e.target.value)}
                />
                <input
                  className="col-span-2 border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
                  placeholder="Category"
                  value={item.category}
                  onChange={(e) => updateItem(idx, 'category', e.target.value)}
                />
                <input
                  type="number"
                  min="1"
                  className="col-span-2 border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
                  placeholder="Qty"
                  value={item.quantity}
                  onChange={(e) => updateItem(idx, 'quantity', parseInt(e.target.value) || 1)}
                />
                <input
                  type="number"
                  min="0"
                  step="0.01"
                  className="col-span-2 border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
                  placeholder="Unit price"
                  value={item.unitPrice}
                  onChange={(e) => updateItem(idx, 'unitPrice', parseFloat(e.target.value) || 0)}
                />
                <div className="col-span-1 text-right text-sm font-medium text-gray-700">
                  ${(item.quantity * item.unitPrice).toFixed(2)}
                </div>
                <button type="button" onClick={() => removeItem(idx)}
                  className="col-span-1 text-red-400 hover:text-red-600 text-lg font-bold text-center"
                >×</button>
              </div>
            ))}
          </div>

          <button type="button" onClick={addItem}
            className="mt-3 text-sm text-sky-600 hover:underline"
          >
            + Add item
          </button>

          <div className="mt-4 pt-4 border-t border-gray-100 flex justify-end">
            <p className="text-lg font-bold text-gray-800">Total: ${total.toFixed(2)}</p>
          </div>
        </div>

        {/* Notes */}
        <div className="bg-white border border-gray-200 rounded-xl p-5">
          <label className="block text-sm font-medium text-gray-700 mb-1">Notes</label>
          <textarea
            rows={2}
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
          />
        </div>

        {error && (
          <p className="text-red-600 text-sm bg-red-50 border border-red-200 rounded-lg px-3 py-2">{error}</p>
        )}

        <div className="flex gap-3">
          <button type="button" onClick={() => navigate(-1)}
            className="px-4 py-2 border border-gray-300 rounded-lg text-sm hover:bg-gray-50">Cancel</button>
          <button type="submit" disabled={loading}
            className="bg-sky-700 hover:bg-sky-800 text-white px-6 py-2 rounded-lg text-sm font-medium disabled:opacity-60">
            {loading ? 'Creating…' : 'Create Bill'}
          </button>
        </div>
      </form>
    </div>
  )
}
