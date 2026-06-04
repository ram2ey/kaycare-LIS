import { useEffect, useState } from 'react'
import { getCatalog, createCatalogItem, updateCatalogItem, deleteCatalogItem } from '../../api/labOrders'
import type { LabTestCatalogItem } from '../../types/labOrders'

export function LabTestCatalogPage() {
  const [catalog, setCatalog] = useState<LabTestCatalogItem[]>([])
  const [search, setSearch] = useState('')
  const [deptFilter, setDeptFilter] = useState('')
  const [statusFilter, setStatusFilter] = useState('')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  // Modals state
  const [isAddModalOpen, setIsAddModalOpen] = useState(false)
  const [editingItem, setEditingItem] = useState<LabTestCatalogItem | null>(null)
  
  // Forms state
  const [form, setForm] = useState({
    testCode: '',
    testName: '',
    department: 'Haematology',
    instrumentType: '',
    isManualEntry: false,
    tatHours: 4,
    defaultUnit: '',
    defaultReferenceRange: '',
    isActive: true,
  })

  useEffect(() => {
    loadCatalog()
  }, [])

  function loadCatalog() {
    setLoading(true)
    getCatalog(true) // includeInactive = true
      .then(setCatalog)
      .catch((err) => setError(err.response?.data?.message || 'Failed to load catalog.'))
      .finally(() => setLoading(false))
  }

  function handleOpenAdd() {
    setForm({
      testCode: '',
      testName: '',
      department: 'Haematology',
      instrumentType: '',
      isManualEntry: false,
      tatHours: 4,
      defaultUnit: '',
      defaultReferenceRange: '',
      isActive: true,
    })
    setError(null)
    setIsAddModalOpen(true)
  }

  function handleOpenEdit(item: LabTestCatalogItem) {
    setForm({
      testCode: item.testCode,
      testName: item.testName,
      department: item.department,
      instrumentType: item.instrumentType ?? '',
      isManualEntry: item.isManualEntry,
      tatHours: item.tatHours,
      defaultUnit: item.defaultUnit ?? '',
      defaultReferenceRange: item.defaultReferenceRange ?? '',
      isActive: item.isActive,
    })
    setError(null)
    setEditingItem(item)
  }

  async function handleAdd(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    try {
      await createCatalogItem({
        testCode: form.testCode.toUpperCase().trim(),
        testName: form.testName.trim(),
        department: form.department,
        instrumentType: form.isManualEntry ? null : form.instrumentType.trim() || null,
        isManualEntry: form.isManualEntry,
        tatHours: Number(form.tatHours),
        defaultUnit: form.defaultUnit.trim() || null,
        defaultReferenceRange: form.defaultReferenceRange.trim() || null,
        isActive: true,
      })
      setIsAddModalOpen(false)
      loadCatalog()
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to add test catalog item.')
    }
  }

  async function handleEdit(e: React.FormEvent) {
    e.preventDefault()
    if (!editingItem) return
    setError(null)
    try {
      await updateCatalogItem(editingItem.labTestCatalogId, {
        testCode: form.testCode.toUpperCase().trim(),
        testName: form.testName.trim(),
        department: form.department,
        instrumentType: form.isManualEntry ? null : form.instrumentType.trim() || null,
        isManualEntry: form.isManualEntry,
        tatHours: Number(form.tatHours),
        defaultUnit: form.defaultUnit.trim() || null,
        defaultReferenceRange: form.defaultReferenceRange.trim() || null,
        isActive: form.isActive,
      })
      setEditingItem(null)
      loadCatalog()
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to update test catalog item.')
    }
  }

  async function handleDelete(item: LabTestCatalogItem) {
    if (!confirm(`Are you sure you want to delete/suspend "${item.testName}"? If it has historical laboratory records, it will be suspended (deactivated) instead of fully deleted.`)) {
      return
    }
    try {
      await deleteCatalogItem(item.labTestCatalogId)
      loadCatalog()
    } catch (err: any) {
      alert(err.response?.data?.message || 'Failed to delete test catalog item.')
    }
  }

  // Derived filter calculations
  const filteredCatalog = catalog.filter((item) => {
    const matchesSearch =
      item.testCode.toLowerCase().includes(search.toLowerCase()) ||
      item.testName.toLowerCase().includes(search.toLowerCase()) ||
      item.department.toLowerCase().includes(search.toLowerCase()) ||
      (item.instrumentType && item.instrumentType.toLowerCase().includes(search.toLowerCase()))

    const matchesDept = !deptFilter || item.department === deptFilter
    const matchesStatus =
      !statusFilter ||
      (statusFilter === 'active' && item.isActive) ||
      (statusFilter === 'inactive' && !item.isActive)

    return matchesSearch && matchesDept && matchesStatus
  })

  // Metric summaries
  const totalCount = catalog.length
  const activeCount = catalog.filter((x) => x.isActive).length
  const manualCount = catalog.filter((x) => x.isManualEntry).length
  const analyzerCount = totalCount - manualCount

  const depts = Array.from(new Set(catalog.map((x) => x.department)))

  const inputCls =
    'w-full border border-gray-200 bg-white rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500'

  return (
    <div className="p-6 max-w-7xl mx-auto">
      {/* Header section */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 tracking-tight">Lab Test Catalog</h1>
          <p className="text-gray-500 text-sm mt-0.5">Manage available laboratory tests, reference ranges, and parameters.</p>
        </div>
        <button
          onClick={handleOpenAdd}
          className="bg-sky-700 hover:bg-sky-800 text-white text-sm font-medium px-4 py-2 rounded-lg shadow-sm transition-all transform hover:-translate-y-0.5 flex items-center justify-center gap-1.5"
        >
          <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
          </svg>
          Add Catalog Test
        </button>
      </div>

      {/* KPI Dashboard */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
        <div className="bg-white border border-gray-100 rounded-xl p-5 shadow-sm flex items-center justify-between">
          <div>
            <p className="text-xs font-semibold text-gray-400 uppercase tracking-wider">Total Catalog Tests</p>
            <p className="text-3xl font-extrabold text-gray-900 mt-1">{totalCount}</p>
            <p className="text-xs text-gray-500 mt-1">Across all clinical departments</p>
          </div>
          <div className="p-3 bg-sky-50 text-sky-700 rounded-lg">
            <svg className="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19.428 15.428a2 2 0 00-1.022-.547l-2.387-.477a6 6 0 00-3.86.517l-.318.158a6 6 0 01-3.86.517L6.05 15.21a2 2 0 00-1.806.547M8 4h8l-1 1v5.172a2 2 0 00.586 1.414l5 5c1.26 1.26.367 3.414-1.415 3.414H4.828c-1.782 0-2.674-2.154-1.414-3.414l5-5A2 2 0 009 10.172V5L8 4z" />
            </svg>
          </div>
        </div>

        <div className="bg-white border border-gray-100 rounded-xl p-5 shadow-sm flex items-center justify-between">
          <div>
            <p className="text-xs font-semibold text-gray-400 uppercase tracking-wider">Active Catalog Items</p>
            <p className="text-3xl font-extrabold text-emerald-600 mt-1">{activeCount}</p>
            <p className="text-xs text-gray-500 mt-1">{totalCount - activeCount} suspended/inactive items</p>
          </div>
          <div className="p-3 bg-emerald-50 text-emerald-700 rounded-lg">
            <svg className="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
          </div>
        </div>

        <div className="bg-white border border-gray-100 rounded-xl p-5 shadow-sm flex items-center justify-between">
          <div>
            <p className="text-xs font-semibold text-gray-400 uppercase tracking-wider">Analyzers vs. Manual</p>
            <p className="text-3xl font-extrabold text-indigo-600 mt-1">{analyzerCount} / {manualCount}</p>
            <p className="text-xs text-gray-500 mt-1">Instrument-driven vs manual entries</p>
          </div>
          <div className="p-3 bg-indigo-50 text-indigo-700 rounded-lg">
            <svg className="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 3v2m6-2v2M9 19v2m6-2v2M5 9H3m2 6H3m18-6h-2m2 6h-2M7 19h10a2 2 0 002-2V7a2 2 0 00-2-2H7a2 2 0 00-2 2v10a2 2 0 002 2zM9 9h6v6H9V9z" />
            </svg>
          </div>
        </div>
      </div>

      {/* Search & filters panel */}
      <div className="bg-white border border-gray-200 rounded-xl p-4 mb-6 shadow-sm flex flex-col md:flex-row gap-4 items-center justify-between">
        <div className="relative w-full md:max-w-md">
          <input
            type="text"
            placeholder="Search test by name, code, department..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="w-full pl-9 pr-4 py-2 border border-gray-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
          />
          <svg className="w-4 h-4 text-gray-400 absolute left-3 top-3" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
          </svg>
        </div>

        <div className="flex w-full md:w-auto items-center gap-3 shrink-0">
          <select
            value={deptFilter}
            onChange={(e) => setDeptFilter(e.target.value)}
            className="w-full md:w-40 border border-gray-200 bg-white rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
          >
            <option value="">All Departments</option>
            {depts.map((d) => (
              <option key={d} value={d}>{d}</option>
            ))}
          </select>

          <select
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value)}
            className="w-full md:w-40 border border-gray-200 bg-white rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
          >
            <option value="">All Statuses</option>
            <option value="active">Active</option>
            <option value="inactive">Suspended</option>
          </select>
        </div>
      </div>

      {/* Catalog table */}
      <div className="bg-white border border-gray-200 rounded-xl overflow-hidden shadow-sm">
        {loading ? (
          <div className="p-12 text-center text-gray-400 text-sm">
            <svg className="w-8 h-8 animate-spin mx-auto text-sky-600 mb-3" fill="none" viewBox="0 0 24 24">
              <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
              <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
            </svg>
            Loading catalog test items...
          </div>
        ) : filteredCatalog.length === 0 ? (
          <div className="p-12 text-center text-gray-400 text-sm">No catalog tests matched the filters.</div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full border-collapse text-left text-sm text-gray-500">
              <thead className="bg-gray-50 text-xs font-semibold text-gray-700 uppercase tracking-wider border-b border-gray-200">
                <tr>
                  <th className="px-6 py-3">Code</th>
                  <th className="px-6 py-3">Test Name</th>
                  <th className="px-6 py-3">Department</th>
                  <th className="px-6 py-3">Type / Instrument</th>
                  <th className="px-6 py-3 text-center">TAT (hrs)</th>
                  <th className="px-6 py-3 text-center">Default Range</th>
                  <th className="px-6 py-3 text-center">Status</th>
                  <th className="px-6 py-3 text-right">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-200 bg-white">
                {filteredCatalog.map((item) => (
                  <tr key={item.labTestCatalogId} className="hover:bg-gray-50 transition-colors">
                    <td className="px-6 py-4 font-mono text-gray-900 font-semibold">{item.testCode}</td>
                    <td className="px-6 py-4 font-medium text-gray-900">{item.testName}</td>
                    <td className="px-6 py-4">
                      <span className="bg-gray-100 text-gray-700 text-xs font-medium px-2 py-0.5 rounded-full">
                        {item.department}
                      </span>
                    </td>
                    <td className="px-6 py-4">
                      {item.isManualEntry ? (
                        <span className="text-gray-400 italic">Manual Entry</span>
                      ) : (
                        <span className="text-gray-700 font-medium">
                          🖥️ {item.instrumentType || 'Analyzer'}
                        </span>
                      )}
                    </td>
                    <td className="px-6 py-4 text-center text-gray-900 font-medium">{item.tatHours} hrs</td>
                    <td className="px-6 py-4 text-center">
                      {item.defaultReferenceRange ? (
                        <span className="font-mono text-xs text-gray-600 bg-gray-50 px-2 py-1 rounded border border-gray-100">
                          {item.defaultReferenceRange} {item.defaultUnit}
                        </span>
                      ) : (
                        <span className="text-gray-300">-</span>
                      )}
                    </td>
                    <td className="px-6 py-4 text-center">
                      <span
                        className={`inline-flex items-center gap-1 text-xs font-medium px-2.5 py-0.5 rounded-full ${
                          item.isActive
                            ? 'bg-emerald-50 text-emerald-700 border border-emerald-200'
                            : 'bg-amber-50 text-amber-700 border border-amber-200'
                        }`}
                      >
                        <span className={`w-1.5 h-1.5 rounded-full ${item.isActive ? 'bg-emerald-500' : 'bg-amber-500'}`} />
                        {item.isActive ? 'Active' : 'Suspended'}
                      </span>
                    </td>
                    <td className="px-6 py-4 text-right">
                      <div className="flex justify-end gap-2">
                        <button
                          onClick={() => handleOpenEdit(item)}
                          className="text-sky-700 hover:text-sky-900 hover:bg-sky-50 px-2.5 py-1.5 rounded-md text-xs font-medium transition-colors border border-transparent hover:border-sky-100"
                        >
                          Edit
                        </button>
                        <button
                          onClick={() => handleDelete(item)}
                          className="text-red-600 hover:text-red-800 hover:bg-red-50 px-2.5 py-1.5 rounded-md text-xs font-medium transition-colors border border-transparent hover:border-red-100"
                        >
                          Delete
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Add Catalog Item Modal */}
      {isAddModalOpen && (
        <div className="fixed inset-0 z-50 overflow-y-auto bg-gray-900/60 backdrop-blur-sm flex items-center justify-center p-4">
          <div className="bg-white rounded-xl border border-gray-200 shadow-2xl max-w-lg w-full overflow-hidden transform transition-all">
            <div className="px-6 py-4 border-b border-gray-150 flex items-center justify-between bg-gray-50">
              <h3 className="font-bold text-gray-900 text-lg">Add Catalog Test</h3>
              <button onClick={() => setIsAddModalOpen(false)} className="text-gray-400 hover:text-gray-600 font-bold text-xl">×</button>
            </div>

            <form onSubmit={handleAdd} className="p-6 space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-bold text-gray-700 uppercase tracking-wider mb-1">Test Code *</label>
                  <input
                    required
                    type="text"
                    placeholder="e.g. CBC"
                    value={form.testCode}
                    onChange={(e) => setForm({ ...form, testCode: e.target.value })}
                    className={inputCls}
                  />
                </div>

                <div>
                  <label className="block text-xs font-bold text-gray-700 uppercase tracking-wider mb-1">Department *</label>
                  <select
                    value={form.department}
                    onChange={(e) => setForm({ ...form, department: e.target.value })}
                    className={inputCls}
                  >
                    <option value="Haematology">Haematology</option>
                    <option value="Chemistry">Chemistry</option>
                    <option value="Microbiology">Microbiology</option>
                    <option value="Immunology">Immunology</option>
                    <option value="Serology">Serology</option>
                    <option value="Urinalysis">Urinalysis</option>
                  </select>
                </div>
              </div>

              <div>
                <label className="block text-xs font-bold text-gray-700 uppercase tracking-wider mb-1">Test Name *</label>
                <input
                  required
                  type="text"
                  placeholder="e.g. Complete Blood Count"
                  value={form.testName}
                  onChange={(e) => setForm({ ...form, testName: e.target.value })}
                  className={inputCls}
                />
              </div>

              <div className="bg-gray-50 border border-gray-200 rounded-lg p-4 space-y-3">
                <div className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    id="isManualEntryAdd"
                    checked={form.isManualEntry}
                    onChange={(e) => setForm({ ...form, isManualEntry: e.target.checked })}
                    className="w-4 h-4 text-sky-600 border-gray-300 rounded focus:ring-sky-500"
                  />
                  <label htmlFor="isManualEntryAdd" className="text-sm font-medium text-gray-700">Manual entry by technician</label>
                </div>

                {!form.isManualEntry && (
                  <div>
                    <label className="block text-xs font-semibold text-gray-600 uppercase tracking-wider mb-1">Instrument / Analyzer Type</label>
                    <input
                      type="text"
                      placeholder="e.g. DxH560, DxC500"
                      value={form.instrumentType}
                      onChange={(e) => setForm({ ...form, instrumentType: e.target.value })}
                      className={inputCls}
                    />
                  </div>
                )}
              </div>

              <div className="grid grid-cols-3 gap-4">
                <div>
                  <label className="block text-xs font-bold text-gray-700 uppercase tracking-wider mb-1">TAT (Hours) *</label>
                  <input
                    required
                    type="number"
                    min={1}
                    value={form.tatHours}
                    onChange={(e) => setForm({ ...form, tatHours: Number(e.target.value) })}
                    className={inputCls}
                  />
                </div>

                <div>
                  <label className="block text-xs font-bold text-gray-700 uppercase tracking-wider mb-1">Default Unit</label>
                  <input
                    type="text"
                    placeholder="e.g. g/dL, mmol/L"
                    value={form.defaultUnit}
                    onChange={(e) => setForm({ ...form, defaultUnit: e.target.value })}
                    className={inputCls}
                  />
                </div>

                <div>
                  <label className="block text-xs font-bold text-gray-700 uppercase tracking-wider mb-1">Default Range</label>
                  <input
                    type="text"
                    placeholder="e.g. 11.5-16.5"
                    value={form.defaultReferenceRange}
                    onChange={(e) => setForm({ ...form, defaultReferenceRange: e.target.value })}
                    className={inputCls}
                  />
                </div>
              </div>

              {error && (
                <div className="bg-red-50 border border-red-200 rounded-lg px-3 py-2 text-xs text-red-700 font-medium">
                  {error}
                </div>
              )}

              <div className="flex justify-end gap-3 pt-4 border-t border-gray-150">
                <button
                  type="button"
                  onClick={() => setIsAddModalOpen(false)}
                  className="px-4 py-2 border border-gray-200 rounded-lg text-sm font-medium hover:bg-gray-50 transition-colors"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="px-4 py-2 bg-sky-700 hover:bg-sky-800 text-white rounded-lg text-sm font-medium shadow-sm transition-all"
                >
                  Save Test
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Edit Catalog Item Modal */}
      {editingItem && (
        <div className="fixed inset-0 z-50 overflow-y-auto bg-gray-900/60 backdrop-blur-sm flex items-center justify-center p-4">
          <div className="bg-white rounded-xl border border-gray-200 shadow-2xl max-w-lg w-full overflow-hidden transform transition-all">
            <div className="px-6 py-4 border-b border-gray-150 flex items-center justify-between bg-gray-50">
              <h3 className="font-bold text-gray-900 text-lg">Edit Catalog Test</h3>
              <button onClick={() => setEditingItem(null)} className="text-gray-400 hover:text-gray-600 font-bold text-xl">×</button>
            </div>

            <form onSubmit={handleEdit} className="p-6 space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-bold text-gray-700 uppercase tracking-wider mb-1">Test Code *</label>
                  <input
                    required
                    type="text"
                    placeholder="e.g. CBC"
                    value={form.testCode}
                    onChange={(e) => setForm({ ...form, testCode: e.target.value })}
                    className={inputCls}
                  />
                </div>

                <div>
                  <label className="block text-xs font-bold text-gray-700 uppercase tracking-wider mb-1">Department *</label>
                  <select
                    value={form.department}
                    onChange={(e) => setForm({ ...form, department: e.target.value })}
                    className={inputCls}
                  >
                    <option value="Haematology">Haematology</option>
                    <option value="Chemistry">Chemistry</option>
                    <option value="Microbiology">Microbiology</option>
                    <option value="Immunology">Immunology</option>
                    <option value="Serology">Serology</option>
                    <option value="Urinalysis">Urinalysis</option>
                  </select>
                </div>
              </div>

              <div>
                <label className="block text-xs font-bold text-gray-700 uppercase tracking-wider mb-1">Test Name *</label>
                <input
                  required
                  type="text"
                  placeholder="e.g. Complete Blood Count"
                  value={form.testName}
                  onChange={(e) => setForm({ ...form, testName: e.target.value })}
                  className={inputCls}
                />
              </div>

              <div className="bg-gray-50 border border-gray-200 rounded-lg p-4 space-y-3">
                <div className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    id="isManualEntryEdit"
                    checked={form.isManualEntry}
                    onChange={(e) => setForm({ ...form, isManualEntry: e.target.checked })}
                    className="w-4 h-4 text-sky-600 border-gray-300 rounded focus:ring-sky-500"
                  />
                  <label htmlFor="isManualEntryEdit" className="text-sm font-medium text-gray-700">Manual entry by technician</label>
                </div>

                {!form.isManualEntry && (
                  <div>
                    <label className="block text-xs font-semibold text-gray-600 uppercase tracking-wider mb-1">Instrument / Analyzer Type</label>
                    <input
                      type="text"
                      placeholder="e.g. DxH560, DxC500"
                      value={form.instrumentType}
                      onChange={(e) => setForm({ ...form, instrumentType: e.target.value })}
                      className={inputCls}
                    />
                  </div>
                )}
              </div>

              <div className="grid grid-cols-3 gap-4">
                <div>
                  <label className="block text-xs font-bold text-gray-700 uppercase tracking-wider mb-1">TAT (Hours) *</label>
                  <input
                    required
                    type="number"
                    min={1}
                    value={form.tatHours}
                    onChange={(e) => setForm({ ...form, tatHours: Number(e.target.value) })}
                    className={inputCls}
                  />
                </div>

                <div>
                  <label className="block text-xs font-bold text-gray-700 uppercase tracking-wider mb-1">Default Unit</label>
                  <input
                    type="text"
                    placeholder="e.g. g/dL, mmol/L"
                    value={form.defaultUnit}
                    onChange={(e) => setForm({ ...form, defaultUnit: e.target.value })}
                    className={inputCls}
                  />
                </div>

                <div>
                  <label className="block text-xs font-bold text-gray-700 uppercase tracking-wider mb-1">Default Range</label>
                  <input
                    type="text"
                    placeholder="e.g. 11.5-16.5"
                    value={form.defaultReferenceRange}
                    onChange={(e) => setForm({ ...form, defaultReferenceRange: e.target.value })}
                    className={inputCls}
                  />
                </div>
              </div>

              <div className="flex items-center gap-2 pt-2">
                <input
                  type="checkbox"
                  id="isActiveEdit"
                  checked={form.isActive}
                  onChange={(e) => setForm({ ...form, isActive: e.target.checked })}
                  className="w-4 h-4 text-sky-600 border-gray-300 rounded focus:ring-sky-500"
                />
                <label htmlFor="isActiveEdit" className="text-sm font-medium text-gray-700">Item is active (available for ordering)</label>
              </div>

              {error && (
                <div className="bg-red-50 border border-red-200 rounded-lg px-3 py-2 text-xs text-red-700 font-medium">
                  {error}
                </div>
              )}

              <div className="flex justify-end gap-3 pt-4 border-t border-gray-150">
                <button
                  type="button"
                  onClick={() => setEditingItem(null)}
                  className="px-4 py-2 border border-gray-200 rounded-lg text-sm font-medium hover:bg-gray-50 transition-colors"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="px-4 py-2 bg-sky-700 hover:bg-sky-800 text-white rounded-lg text-sm font-medium shadow-sm transition-all"
                >
                  Save Changes
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  )
}
