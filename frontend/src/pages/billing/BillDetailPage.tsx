import { useEffect, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { getBill, issueBill, voidBill, addPayment, adjustBill, downloadInvoice, downloadReceipt } from '../../api/billing'
import type { BillDetail } from '../../types/billing'
import { BILL_STATUS_COLORS, PAYMENT_METHODS } from '../../types/billing'
import { useAuth } from '../../context/AuthContext'

export function BillDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { user } = useAuth()
  const [bill, setBill] = useState<BillDetail | null>(null)
  const [loading, setLoading] = useState(true)
  const [showPayment, setShowPayment] = useState(false)
  const [showAdjust, setShowAdjust] = useState(false)
  const [payForm, setPayForm] = useState({ amount: '', paymentMethod: 'Cash', reference: '', notes: '' })
  const [adjForm, setAdjForm] = useState({ adjustmentAmount: '', reason: '' })
  const [submitting, setSubmitting] = useState(false)

  function load() {
    if (!id) return
    setLoading(true)
    getBill(id).then(setBill).finally(() => setLoading(false))
  }

  useEffect(() => { load() }, [id])

  async function handleIssue() {
    if (!id || !confirm('Issue this bill?')) return
    await issueBill(id)
    load()
  }

  async function handleVoid() {
    if (!id || !confirm('Void this bill? This cannot be undone.')) return
    await voidBill(id)
    load()
  }

  async function handlePayment(e: React.FormEvent) {
    e.preventDefault()
    if (!id) return
    setSubmitting(true)
    try {
      await addPayment(id, {
        amount: parseFloat(payForm.amount),
        paymentMethod: payForm.paymentMethod,
        reference: payForm.reference || undefined,
        notes: payForm.notes || undefined,
      })
      setShowPayment(false)
      setPayForm({ amount: '', paymentMethod: 'Cash', reference: '', notes: '' })
      load()
    } finally {
      setSubmitting(false)
    }
  }

  async function handleAdjust(e: React.FormEvent) {
    e.preventDefault()
    if (!id) return
    setSubmitting(true)
    try {
      await adjustBill(id, {
        adjustmentAmount: parseFloat(adjForm.adjustmentAmount),
        reason: adjForm.reason,
      })
      setShowAdjust(false)
      setAdjForm({ adjustmentAmount: '', reason: '' })
      load()
    } finally {
      setSubmitting(false)
    }
  }

  async function handleDownloadInvoice() {
    if (!id) return
    const blob = await downloadInvoice(id)
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `invoice-${bill?.billNumber}.pdf`
    a.click()
    URL.revokeObjectURL(url)
  }

  async function handleDownloadReceipt(paymentId: string) {
    const blob = await downloadReceipt(paymentId)
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `receipt-${paymentId}.pdf`
    a.click()
    URL.revokeObjectURL(url)
  }

  if (loading) return <div className="p-6 text-gray-400 text-sm">Loading…</div>
  if (!bill) return <div className="p-6 text-red-600 text-sm">Bill not found.</div>

  const canModify = ['SuperAdmin', 'Admin', 'BillingOfficer', 'Receptionist'].includes(user?.role ?? '')
  const canVoid = ['SuperAdmin', 'Admin'].includes(user?.role ?? '')
  const isDraft = bill.status === 'Draft'
  const isActive = bill.status === 'Issued' || bill.status === 'PartiallyPaid'

  return (
    <div className="p-6 max-w-4xl">
      <button onClick={() => navigate(-1)} className="text-sm text-gray-500 hover:text-gray-700 mb-4">← Back</button>

      <div className="flex items-start justify-between mb-6">
        <div>
          <div className="flex items-center gap-3 mb-1">
            <h1 className="text-2xl font-bold text-gray-900 font-mono">{bill.billNumber}</h1>
            <span className={`px-2.5 py-1 rounded text-xs font-medium ${BILL_STATUS_COLORS[bill.status] ?? 'bg-gray-100 text-gray-600'}`}>
              {bill.status}
            </span>
          </div>
          <p className="text-gray-600 text-sm">{bill.patientName} · <span className="font-mono text-xs">{bill.mrn}</span></p>
          <p className="text-gray-400 text-xs mt-0.5">{new Date(bill.createdAt).toLocaleString('en-GB')}</p>
        </div>
        <div className="flex gap-2">
          {isDraft && canModify && (
            <button onClick={handleIssue} className="bg-blue-600 hover:bg-blue-700 text-white px-3 py-1.5 rounded-lg text-sm font-medium">
              Issue Bill
            </button>
          )}
          {isActive && canModify && (
            <>
              <button onClick={() => setShowPayment(true)} className="bg-green-600 hover:bg-green-700 text-white px-3 py-1.5 rounded-lg text-sm font-medium">
                + Payment
              </button>
              <button onClick={() => setShowAdjust(true)} className="bg-orange-500 hover:bg-orange-600 text-white px-3 py-1.5 rounded-lg text-sm font-medium">
                Adjust
              </button>
            </>
          )}
          {canVoid && bill.status !== 'Voided' && (
            <button onClick={handleVoid} className="border border-red-300 text-red-600 hover:bg-red-50 px-3 py-1.5 rounded-lg text-sm">
              Void
            </button>
          )}
          <button onClick={handleDownloadInvoice} className="bg-sky-700 hover:bg-sky-800 text-white px-3 py-1.5 rounded-lg text-sm font-medium">
            Download Invoice
          </button>
        </div>
      </div>

      {/* Totals */}
      <div className="grid grid-cols-4 gap-4 mb-6">
        <div className="bg-white border border-gray-200 rounded-xl p-4 text-center">
          <p className="text-xs text-gray-500 mb-1">Total</p>
          <p className="text-xl font-bold text-gray-800">${bill.totalAmount.toFixed(2)}</p>
        </div>
        {bill.discountAmount > 0 && (
          <div className="bg-white border border-gray-200 rounded-xl p-4 text-center">
            <p className="text-xs text-gray-500 mb-1">Discount</p>
            <p className="text-xl font-bold text-purple-600">-${bill.discountAmount.toFixed(2)}</p>
          </div>
        )}
        <div className="bg-white border border-gray-200 rounded-xl p-4 text-center">
          <p className="text-xs text-gray-500 mb-1">Paid</p>
          <p className="text-xl font-bold text-green-600">${bill.paidAmount.toFixed(2)}</p>
        </div>
        <div className={`border rounded-xl p-4 text-center ${bill.balanceDue > 0 ? 'bg-red-50 border-red-200' : 'bg-green-50 border-green-200'}`}>
          <p className="text-xs text-gray-500 mb-1">Balance Due</p>
          <p className={`text-xl font-bold ${bill.balanceDue > 0 ? 'text-red-600' : 'text-green-600'}`}>
            ${bill.balanceDue.toFixed(2)}
          </p>
        </div>
      </div>

      {/* Line items */}
      <div className="bg-white border border-gray-200 rounded-xl overflow-hidden mb-6">
        <div className="px-5 py-4 border-b border-gray-100">
          <h2 className="font-semibold text-gray-800">Line Items</h2>
        </div>
        <table className="w-full text-sm">
          <thead>
            <tr className="text-left text-xs text-gray-500 border-b border-gray-100 bg-gray-50">
              <th className="px-5 py-3 font-medium">Description</th>
              <th className="px-5 py-3 font-medium">Category</th>
              <th className="px-5 py-3 font-medium">Qty</th>
              <th className="px-5 py-3 font-medium">Unit Price</th>
              <th className="px-5 py-3 font-medium text-right">Total</th>
            </tr>
          </thead>
          <tbody>
            {bill.items.map((item) => (
              <tr key={item.billItemId} className="border-b border-gray-50">
                <td className="px-5 py-3 text-gray-800 font-medium">{item.description}</td>
                <td className="px-5 py-3 text-gray-500">{item.category ?? '—'}</td>
                <td className="px-5 py-3 text-gray-600">{item.quantity}</td>
                <td className="px-5 py-3 text-gray-600">${item.unitPrice.toFixed(2)}</td>
                <td className="px-5 py-3 text-gray-800 font-medium text-right">${item.totalPrice.toFixed(2)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Payments */}
      <div className="bg-white border border-gray-200 rounded-xl overflow-hidden mb-6">
        <div className="px-5 py-4 border-b border-gray-100">
          <h2 className="font-semibold text-gray-800">Payments</h2>
        </div>
        {bill.payments.length === 0 ? (
          <p className="px-5 py-6 text-sm text-gray-400 text-center">No payments recorded.</p>
        ) : (
          <table className="w-full text-sm">
            <thead>
              <tr className="text-left text-xs text-gray-500 border-b border-gray-100 bg-gray-50">
                <th className="px-5 py-3 font-medium">Amount</th>
                <th className="px-5 py-3 font-medium">Method</th>
                <th className="px-5 py-3 font-medium">Reference</th>
                <th className="px-5 py-3 font-medium">By</th>
                <th className="px-5 py-3 font-medium">Date</th>
                <th className="px-5 py-3 font-medium">Receipt</th>
              </tr>
            </thead>
            <tbody>
              {bill.payments.map((p) => (
                <tr key={p.paymentId} className="border-b border-gray-50">
                  <td className="px-5 py-3 font-medium text-green-700">${p.amount.toFixed(2)}</td>
                  <td className="px-5 py-3 text-gray-600">{p.paymentMethod}</td>
                  <td className="px-5 py-3 text-gray-500">{p.reference ?? '—'}</td>
                  <td className="px-5 py-3 text-gray-500">{p.recordedByName}</td>
                  <td className="px-5 py-3 text-gray-500 text-xs">
                    {new Date(p.paidAt).toLocaleString('en-GB')}
                  </td>
                  <td className="px-5 py-3">
                    <button
                      onClick={() => handleDownloadReceipt(p.paymentId)}
                      className="text-sky-600 hover:underline text-xs"
                    >
                      Receipt
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {/* Payment modal */}
      {showPayment && (
        <div className="fixed inset-0 bg-black/30 flex items-center justify-center z-50">
          <div className="bg-white rounded-2xl shadow-xl p-6 w-full max-w-sm">
            <h3 className="font-semibold text-gray-800 mb-4">Record Payment</h3>
            <form onSubmit={handlePayment} className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Amount</label>
                <input required type="number" step="0.01" min="0.01"
                  placeholder={bill.balanceDue.toFixed(2)}
                  value={payForm.amount}
                  onChange={(e) => setPayForm({ ...payForm, amount: e.target.value })}
                  className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Payment Method</label>
                <select value={payForm.paymentMethod}
                  onChange={(e) => setPayForm({ ...payForm, paymentMethod: e.target.value })}
                  className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
                >
                  {PAYMENT_METHODS.map((m) => <option key={m}>{m}</option>)}
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Reference</label>
                <input value={payForm.reference}
                  onChange={(e) => setPayForm({ ...payForm, reference: e.target.value })}
                  placeholder="Transaction ID, cheque #, etc."
                  className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
                />
              </div>
              <div className="flex gap-3 pt-2">
                <button type="button" onClick={() => setShowPayment(false)}
                  className="flex-1 border border-gray-300 rounded-lg py-2 text-sm hover:bg-gray-50">Cancel</button>
                <button type="submit" disabled={submitting}
                  className="flex-1 bg-green-600 text-white rounded-lg py-2 text-sm font-medium hover:bg-green-700 disabled:opacity-60">
                  {submitting ? 'Saving…' : 'Record'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Adjust modal */}
      {showAdjust && (
        <div className="fixed inset-0 bg-black/30 flex items-center justify-center z-50">
          <div className="bg-white rounded-2xl shadow-xl p-6 w-full max-w-sm">
            <h3 className="font-semibold text-gray-800 mb-4">Adjust Bill</h3>
            <form onSubmit={handleAdjust} className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Adjustment Amount</label>
                <input required type="number" step="0.01"
                  placeholder="Positive = credit, negative = charge"
                  value={adjForm.adjustmentAmount}
                  onChange={(e) => setAdjForm({ ...adjForm, adjustmentAmount: e.target.value })}
                  className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Reason</label>
                <input required value={adjForm.reason}
                  onChange={(e) => setAdjForm({ ...adjForm, reason: e.target.value })}
                  className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
                />
              </div>
              <div className="flex gap-3 pt-2">
                <button type="button" onClick={() => setShowAdjust(false)}
                  className="flex-1 border border-gray-300 rounded-lg py-2 text-sm hover:bg-gray-50">Cancel</button>
                <button type="submit" disabled={submitting}
                  className="flex-1 bg-orange-500 text-white rounded-lg py-2 text-sm font-medium hover:bg-orange-600 disabled:opacity-60">
                  {submitting ? 'Saving…' : 'Adjust'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  )
}
