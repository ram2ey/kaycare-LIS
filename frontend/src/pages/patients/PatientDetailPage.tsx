import { useEffect, useState } from 'react'
import { useParams, useNavigate, Link } from 'react-router-dom'
import { getPatient } from '../../api/patients'
import { getLabOrdersForPatient } from '../../api/labOrders'
import { getBills } from '../../api/billing'
import { getDocuments, uploadDocument, deleteDocument } from '../../api/documents'
import type { PatientDetail } from '../../types/patients'
import type { LabOrderSummary } from '../../types/labOrders'
import type { BillSummary } from '../../types/billing'
import type { DocumentResponse } from '../../api/documents'
import { useAuth } from '../../context/AuthContext'
import { ORDER_STATUS_COLORS } from '../../types/labOrders'
import { BILL_STATUS_COLORS as BSC } from '../../types/billing'

type Tab = 'overview' | 'lab' | 'billing' | 'documents'

export function PatientDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { user } = useAuth()
  const [tab, setTab] = useState<Tab>('overview')
  const [patient, setPatient] = useState<PatientDetail | null>(null)
  const [labOrders, setLabOrders] = useState<LabOrderSummary[]>([])
  const [bills, setBills] = useState<BillSummary[]>([])
  const [documents, setDocuments] = useState<DocumentResponse[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    if (!id) return
    Promise.all([
      getPatient(id).then(setPatient),
      getLabOrdersForPatient(id).then(setLabOrders),
      getBills(id).then(setBills),
      getDocuments(id).then(setDocuments),
    ]).finally(() => setLoading(false))
  }, [id])

  function handleFileUpload(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0]
    if (!file || !id) return
    uploadDocument(id, file).then((doc) => setDocuments((d) => [...d, doc]))
    e.target.value = ''
  }

  async function handleDeleteDoc(docId: string) {
    if (!confirm('Delete this document?')) return
    await deleteDocument(docId)
    setDocuments((d) => d.filter((doc) => doc.documentId !== docId))
  }

  if (loading) {
    return <div className="p-6 text-gray-400 text-sm">Loading…</div>
  }

  if (!patient) {
    return <div className="p-6 text-red-600 text-sm">Patient not found.</div>
  }

  const isAdmin = ['SuperAdmin', 'Admin'].includes(user?.role ?? '')
  const canBill = ['SuperAdmin', 'Admin', 'BillingOfficer', 'Receptionist'].includes(user?.role ?? '')
  const canOrder = ['SuperAdmin', 'Admin', 'Doctor'].includes(user?.role ?? '')

  const outstandingBalance = bills
    .filter((b) => b.status !== 'Paid' && b.status !== 'Voided' && b.status !== 'Waived')
    .reduce((sum, b) => sum + b.balanceDue, 0)

  return (
    <div className="p-6">
      {/* Header */}
      <div className="mb-6">
        <button onClick={() => navigate(-1)} className="text-sm text-gray-500 hover:text-gray-700 mb-2">← Back</button>
        <div className="flex items-start justify-between">
          <div>
            <h1 className="text-2xl font-bold text-gray-900">
              {patient.firstName} {patient.lastName}
            </h1>
            <p className="text-gray-500 text-sm mt-0.5">
              MRN: <span className="font-mono">{patient.mrn}</span> · {patient.gender} ·{' '}
              {new Date(patient.dateOfBirth).toLocaleDateString('en-GB')}
            </p>
          </div>
          <div className="flex gap-2">
            {canOrder && (
              <Link
                to={`/lab-orders/new?patientId=${patient.patientId}`}
                className="bg-sky-700 text-white px-3 py-1.5 rounded-lg text-sm hover:bg-sky-800 transition-colors"
              >
                + Lab Order
              </Link>
            )}
            {canBill && (
              <Link
                to={`/billing/new?patientId=${patient.patientId}`}
                className="bg-green-700 text-white px-3 py-1.5 rounded-lg text-sm hover:bg-green-800 transition-colors"
              >
                + Bill
              </Link>
            )}
          </div>
        </div>
      </div>

      {/* Tabs */}
      <div className="flex gap-1 mb-6 border-b border-gray-200">
        {(['overview', 'lab', 'billing', 'documents'] as Tab[]).map((t) => (
          <button
            key={t}
            onClick={() => setTab(t)}
            className={`px-4 py-2 text-sm font-medium capitalize border-b-2 transition-colors ${
              tab === t
                ? 'border-sky-600 text-sky-700'
                : 'border-transparent text-gray-500 hover:text-gray-700'
            }`}
          >
            {t === 'lab' ? 'Lab Orders' : t === 'billing' ? 'Billing' : t.charAt(0).toUpperCase() + t.slice(1)}
          </button>
        ))}
      </div>

      {/* Overview */}
      {tab === 'overview' && (
        <div className="grid grid-cols-2 gap-6">
          <InfoCard title="Demographics">
            <InfoRow label="Phone" value={patient.phone} />
            <InfoRow label="Email" value={patient.email} />
            <InfoRow label="Address" value={patient.address} />
            <InfoRow label="Blood Group" value={patient.bloodGroup} />
            <InfoRow label="NHIS Number" value={patient.nhisNumber} />
          </InfoCard>
          <InfoCard title="Emergency Contact">
            <InfoRow label="Name" value={patient.emergencyContactName} />
            <InfoRow label="Phone" value={patient.emergencyContactPhone} />
          </InfoCard>
          <InfoCard title="Summary">
            <InfoRow label="Lab Orders" value={String(labOrders.length)} />
            <InfoRow label="Bills" value={String(bills.length)} />
            <InfoRow
              label="Outstanding Balance"
              value={`$${outstandingBalance.toFixed(2)}`}
              highlight={outstandingBalance > 0}
            />
          </InfoCard>
          {patient.notes && (
            <InfoCard title="Notes">
              <p className="text-sm text-gray-700 whitespace-pre-wrap">{patient.notes}</p>
            </InfoCard>
          )}
        </div>
      )}

      {/* Lab Orders */}
      {tab === 'lab' && (
        <div className="bg-white border border-gray-200 rounded-xl overflow-hidden">
          {labOrders.length === 0 ? (
            <p className="py-12 text-center text-gray-400 text-sm">No lab orders yet.</p>
          ) : (
            <table className="w-full text-sm">
              <thead>
                <tr className="text-left text-xs text-gray-500 border-b border-gray-100 bg-gray-50">
                  <th className="px-5 py-3 font-medium">Accession</th>
                  <th className="px-5 py-3 font-medium">Tests</th>
                  <th className="px-5 py-3 font-medium">Priority</th>
                  <th className="px-5 py-3 font-medium">Status</th>
                  <th className="px-5 py-3 font-medium">Date</th>
                </tr>
              </thead>
              <tbody>
                {labOrders.map((o) => (
                  <tr key={o.labOrderId} className="border-b border-gray-50 hover:bg-gray-50">
                    <td className="px-5 py-3">
                      <Link to={`/lab-orders/${o.labOrderId}`} className="text-sky-600 hover:underline font-mono text-xs">
                        {o.accessionNumber}
                      </Link>
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
          )}
        </div>
      )}

      {/* Billing */}
      {tab === 'billing' && (
        <div className="bg-white border border-gray-200 rounded-xl overflow-hidden">
          {bills.length === 0 ? (
            <p className="py-12 text-center text-gray-400 text-sm">No bills yet.</p>
          ) : (
            <table className="w-full text-sm">
              <thead>
                <tr className="text-left text-xs text-gray-500 border-b border-gray-100 bg-gray-50">
                  <th className="px-5 py-3 font-medium">Bill #</th>
                  <th className="px-5 py-3 font-medium">Total</th>
                  <th className="px-5 py-3 font-medium">Paid</th>
                  <th className="px-5 py-3 font-medium">Balance</th>
                  <th className="px-5 py-3 font-medium">Status</th>
                  <th className="px-5 py-3 font-medium">Date</th>
                </tr>
              </thead>
              <tbody>
                {bills.map((b) => (
                  <tr key={b.billId} className="border-b border-gray-50 hover:bg-gray-50">
                    <td className="px-5 py-3">
                      <Link to={`/billing/${b.billId}`} className="text-sky-600 hover:underline font-mono text-xs">
                        {b.billNumber}
                      </Link>
                    </td>
                    <td className="px-5 py-3 text-gray-700">${b.totalAmount.toFixed(2)}</td>
                    <td className="px-5 py-3 text-green-700">${b.paidAmount.toFixed(2)}</td>
                    <td className={`px-5 py-3 font-medium ${b.balanceDue > 0 ? 'text-red-600' : 'text-green-600'}`}>
                      ${b.balanceDue.toFixed(2)}
                    </td>
                    <td className="px-5 py-3">
                      <span className={`px-2 py-0.5 rounded text-xs font-medium ${BSC[b.status] ?? 'bg-gray-100 text-gray-600'}`}>
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
      )}

      {/* Documents */}
      {tab === 'documents' && (
        <div>
          <div className="flex items-center justify-between mb-4">
            <p className="text-sm text-gray-500">{documents.length} document{documents.length !== 1 ? 's' : ''}</p>
            <label className="bg-sky-700 text-white px-3 py-1.5 rounded-lg text-sm cursor-pointer hover:bg-sky-800 transition-colors">
              Upload
              <input type="file" className="hidden" onChange={handleFileUpload} />
            </label>
          </div>
          <div className="bg-white border border-gray-200 rounded-xl overflow-hidden">
            {documents.length === 0 ? (
              <p className="py-12 text-center text-gray-400 text-sm">No documents uploaded.</p>
            ) : (
              <table className="w-full text-sm">
                <thead>
                  <tr className="text-left text-xs text-gray-500 border-b border-gray-100 bg-gray-50">
                    <th className="px-5 py-3 font-medium">File</th>
                    <th className="px-5 py-3 font-medium">Type</th>
                    <th className="px-5 py-3 font-medium">Size</th>
                    <th className="px-5 py-3 font-medium">Uploaded By</th>
                    <th className="px-5 py-3 font-medium">Date</th>
                    <th className="px-5 py-3 font-medium">Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {documents.map((doc) => (
                    <tr key={doc.documentId} className="border-b border-gray-50 hover:bg-gray-50">
                      <td className="px-5 py-3 text-sky-700 font-medium">{doc.fileName}</td>
                      <td className="px-5 py-3 text-gray-500">{doc.documentType ?? '—'}</td>
                      <td className="px-5 py-3 text-gray-500">{(doc.fileSizeBytes / 1024).toFixed(0)} KB</td>
                      <td className="px-5 py-3 text-gray-500">{doc.uploadedByName}</td>
                      <td className="px-5 py-3 text-gray-500 text-xs">
                        {new Date(doc.createdAt).toLocaleDateString('en-GB')}
                      </td>
                      <td className="px-5 py-3">
                        <a
                          href={doc.downloadUrl}
                          target="_blank"
                          rel="noopener noreferrer"
                          className="text-sky-600 hover:underline text-xs mr-3"
                        >
                          Download
                        </a>
                        {isAdmin && (
                          <button
                            onClick={() => handleDeleteDoc(doc.documentId)}
                            className="text-red-500 hover:underline text-xs"
                          >
                            Delete
                          </button>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>
        </div>
      )}
    </div>
  )
}

function InfoCard({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="bg-white border border-gray-200 rounded-xl p-5">
      <h3 className="font-semibold text-gray-700 text-sm mb-3">{title}</h3>
      <div className="space-y-2">{children}</div>
    </div>
  )
}

function InfoRow({ label, value, highlight }: { label: string; value: string | null | undefined; highlight?: boolean }) {
  return (
    <div className="flex justify-between text-sm">
      <span className="text-gray-500">{label}</span>
      <span className={`font-medium ${highlight ? 'text-red-600' : 'text-gray-800'}`}>{value ?? '—'}</span>
    </div>
  )
}
