import { useEffect, useState } from 'react'
import { getAuditLogs } from '../../api/audit'
import type { AuditLogResponse } from '../../types/audit'

export function AuditLogsPage() {
  const [logs, setLogs] = useState<AuditLogResponse[]>([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [filters, setFilters] = useState({ action: '', from: '', to: '' })
  const [loading, setLoading] = useState(true)
  const pageSize = 50

  function load(p: number) {
    setLoading(true)
    getAuditLogs({
      action: filters.action || undefined,
      from: filters.from || undefined,
      to: filters.to || undefined,
      page: p,
      pageSize,
    })
      .then((r) => { setLogs(r.items); setTotal(r.total) })
      .finally(() => setLoading(false))
  }

  useEffect(() => { load(page) }, [page])

  function handleFilter(e: React.FormEvent) {
    e.preventDefault()
    setPage(1)
    load(1)
  }

  const totalPages = Math.ceil(total / pageSize)

  return (
    <div className="p-6">
      <h1 className="text-2xl font-bold text-gray-900 mb-6">Audit Logs</h1>

      <form onSubmit={handleFilter} className="flex gap-3 mb-4">
        <input
          placeholder="Filter by action…"
          value={filters.action}
          onChange={(e) => setFilters({ ...filters, action: e.target.value })}
          className="border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500 w-48"
        />
        <input
          type="datetime-local"
          value={filters.from}
          onChange={(e) => setFilters({ ...filters, from: e.target.value })}
          className="border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
        />
        <input
          type="datetime-local"
          value={filters.to}
          onChange={(e) => setFilters({ ...filters, to: e.target.value })}
          className="border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
        />
        <button type="submit" className="bg-sky-700 text-white px-4 py-2 rounded-lg text-sm hover:bg-sky-800">
          Filter
        </button>
      </form>

      <p className="text-sm text-gray-500 mb-3">{total} entries</p>

      <div className="bg-white border border-gray-200 rounded-xl overflow-hidden">
        {loading ? (
          <p className="py-12 text-center text-gray-400 text-sm">Loading…</p>
        ) : logs.length === 0 ? (
          <p className="py-12 text-center text-gray-400 text-sm">No audit logs.</p>
        ) : (
          <table className="w-full text-sm">
            <thead>
              <tr className="text-left text-xs text-gray-500 border-b border-gray-100 bg-gray-50">
                <th className="px-5 py-3 font-medium">Timestamp</th>
                <th className="px-5 py-3 font-medium">Action</th>
                <th className="px-5 py-3 font-medium">User</th>
                <th className="px-5 py-3 font-medium">Entity</th>
                <th className="px-5 py-3 font-medium">Details</th>
                <th className="px-5 py-3 font-medium">IP</th>
              </tr>
            </thead>
            <tbody>
              {logs.map((log) => (
                <tr key={log.auditLogId} className="border-b border-gray-50 hover:bg-gray-50">
                  <td className="px-5 py-3 text-gray-500 text-xs whitespace-nowrap">
                    {new Date(log.timestamp).toLocaleString('en-GB')}
                  </td>
                  <td className="px-5 py-3">
                    <span className="bg-blue-100 text-blue-700 px-2 py-0.5 rounded text-xs font-medium">
                      {log.action}
                    </span>
                  </td>
                  <td className="px-5 py-3 text-gray-600 text-xs">{log.userEmail ?? '—'}</td>
                  <td className="px-5 py-3 text-gray-500 text-xs">{log.entityType ?? '—'}</td>
                  <td className="px-5 py-3 text-gray-500 text-xs max-w-xs truncate">{log.details ?? '—'}</td>
                  <td className="px-5 py-3 text-gray-400 text-xs font-mono">{log.ipAddress ?? '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {totalPages > 1 && (
        <div className="flex items-center justify-between mt-4">
          <p className="text-sm text-gray-500">Page {page} of {totalPages}</p>
          <div className="flex gap-2">
            <button disabled={page === 1} onClick={() => setPage((p) => p - 1)}
              className="px-3 py-1.5 text-sm border border-gray-300 rounded-lg hover:bg-gray-50 disabled:opacity-50">
              ← Prev
            </button>
            <button disabled={page === totalPages} onClick={() => setPage((p) => p + 1)}
              className="px-3 py-1.5 text-sm border border-gray-300 rounded-lg hover:bg-gray-50 disabled:opacity-50">
              Next →
            </button>
          </div>
        </div>
      )}
    </div>
  )
}
