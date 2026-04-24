import { useState } from 'react'
import { Link } from 'react-router-dom'
import { getResultByAccession } from '../../api/labResults'
import type { LabResultSummary } from '../../types/labResults'
import { FLAG_COLORS } from '../../types/labOrders'

export function Hl7InboxPage() {
  const [accession, setAccession] = useState('')
  const [result, setResult] = useState<LabResultSummary | null>(null)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  async function handleSearch(e: React.FormEvent) {
    e.preventDefault()
    if (!accession.trim()) return
    setError('')
    setLoading(true)
    try {
      const res = await getResultByAccession(accession.trim())
      setResult(res)
    } catch {
      setError(`No HL7 result found for accession: ${accession}`)
      setResult(null)
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="p-6">
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900">HL7 Results Inbox</h1>
        <p className="text-gray-500 text-sm mt-0.5">
          Results received from lab instruments via MLLP on port 2575
        </p>
      </div>

      <form onSubmit={handleSearch} className="flex gap-2 mb-6 max-w-sm">
        <input
          type="text"
          placeholder="Accession number (ACC-2026-00001)"
          value={accession}
          onChange={(e) => setAccession(e.target.value)}
          className="flex-1 border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500 font-mono"
        />
        <button
          type="submit"
          disabled={loading}
          className="bg-sky-700 text-white px-4 py-2 rounded-lg text-sm hover:bg-sky-800 transition-colors disabled:opacity-60"
        >
          {loading ? '…' : 'Lookup'}
        </button>
      </form>

      {error && (
        <div className="bg-red-50 border border-red-200 rounded-xl px-4 py-3 text-sm text-red-700 mb-4">
          {error}
        </div>
      )}

      {result && (
        <div className="bg-white border border-gray-200 rounded-xl p-6">
          <div className="flex items-start justify-between mb-4">
            <div>
              <p className="text-xs text-gray-500 font-mono mb-1">{result.accessionNumber}</p>
              <h2 className="font-semibold text-gray-800">{result.patientName}</h2>
              <p className="text-sm text-gray-500 font-mono">{result.mrn}</p>
            </div>
            <div className="text-right">
              <span className="bg-blue-100 text-blue-700 px-2 py-0.5 rounded text-xs font-medium">
                {result.source}
              </span>
              <p className="text-xs text-gray-400 mt-1">
                {new Date(result.receivedAt).toLocaleString('en-GB')}
              </p>
            </div>
          </div>

          <Link
            to={`/lab-orders/${result.labOrderId}`}
            className="text-sky-600 hover:underline text-sm mb-4 block"
          >
            View Lab Order →
          </Link>

          <table className="w-full text-sm">
            <thead>
              <tr className="text-left text-xs text-gray-500 border-b border-gray-100 bg-gray-50">
                <th className="px-4 py-2 font-medium">Test</th>
                <th className="px-4 py-2 font-medium">Result</th>
                <th className="px-4 py-2 font-medium">Unit</th>
                <th className="px-4 py-2 font-medium">Ref Range</th>
                <th className="px-4 py-2 font-medium">Flag</th>
                <th className="px-4 py-2 font-medium">Observed At</th>
              </tr>
            </thead>
            <tbody>
              {result.observations.map((obs) => (
                <tr key={obs.labObservationId} className="border-b border-gray-50">
                  <td className="px-4 py-2">
                    <p className="font-medium text-gray-800">{obs.testName}</p>
                    <p className="text-xs text-gray-400 font-mono">{obs.testCode}</p>
                  </td>
                  <td className="px-4 py-2 font-medium text-gray-800">{obs.resultValue}</td>
                  <td className="px-4 py-2 text-gray-500">{obs.unit ?? '—'}</td>
                  <td className="px-4 py-2 text-gray-500 text-xs">{obs.referenceRange ?? '—'}</td>
                  <td className="px-4 py-2">
                    {obs.flag ? (
                      <span className={`font-bold ${FLAG_COLORS[obs.flag] ?? ''}`}>{obs.flag}</span>
                    ) : (
                      <span className="text-gray-300">—</span>
                    )}
                  </td>
                  <td className="px-4 py-2 text-gray-500 text-xs">
                    {new Date(obs.observedAt).toLocaleString('en-GB')}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>

          {result.rawHl7Message && (
            <details className="mt-4">
              <summary className="text-xs text-gray-400 cursor-pointer hover:text-gray-600">
                Raw HL7 Message
              </summary>
              <pre className="mt-2 bg-gray-50 border border-gray-200 rounded p-3 text-xs text-gray-600 overflow-auto whitespace-pre-wrap font-mono">
                {result.rawHl7Message}
              </pre>
            </details>
          )}
        </div>
      )}
    </div>
  )
}
