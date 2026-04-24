export interface LabTestCatalogItem {
  labTestCatalogId: string
  testCode: string
  testName: string
  department: string
  sampleType: string
  turnaroundHours: number
  defaultUnit: string | null
  defaultReferenceRange: string | null
  isActive: boolean
}

export interface LabOrderItemResponse {
  labOrderItemId: string
  labTestCatalogId: string
  testCode: string
  testName: string
  department: string
  sampleType: string
  status: string
  manualResultValue: string | null
  manualResultUnit: string | null
  manualResultReferenceRange: string | null
  manualResultFlag: string | null
  manualResultNotes: string | null
  resultedAt: string | null
  signedAt: string | null
  signedByUserId: string | null
  signedByName: string | null
  hl7ResultValue: string | null
  hl7ResultUnit: string | null
  hl7Flag: string | null
  hl7ResultedAt: string | null
}

export interface LabObservationResponse {
  labObservationId: string
  testCode: string
  testName: string
  resultValue: string
  unit: string | null
  referenceRange: string | null
  flag: string | null
  observedAt: string
}

export interface LabOrderSummary {
  labOrderId: string
  accessionNumber: string
  patientId: string
  patientName: string
  mrn: string
  orderingDoctorUserId: string
  orderingDoctorName: string
  status: string
  priority: string
  sampleCollectedAt: string | null
  completedAt: string | null
  createdAt: string
  items: LabOrderItemResponse[]
}

export interface LabOrderDetail extends LabOrderSummary {
  clinicalNotes: string | null
  billId: string | null
}

export interface CreateLabOrderRequest {
  patientId: string
  priority?: string
  clinicalNotes?: string
  items: { labTestCatalogId: string }[]
}

export interface ManualResultRequest {
  value: string
  unit?: string
  referenceRange?: string
  notes?: string
}

export const ORDER_STATUS_COLORS: Record<string, string> = {
  Draft: 'bg-gray-100 text-gray-600',
  Ordered: 'bg-blue-100 text-blue-700',
  SampleCollected: 'bg-yellow-100 text-yellow-700',
  InProgress: 'bg-orange-100 text-orange-700',
  PartiallyCompleted: 'bg-purple-100 text-purple-700',
  Completed: 'bg-green-100 text-green-700',
  Cancelled: 'bg-red-100 text-red-700',
}

export const ITEM_STATUS_COLORS: Record<string, string> = {
  Pending: 'bg-gray-100 text-gray-600',
  SampleCollected: 'bg-yellow-100 text-yellow-700',
  Resulted: 'bg-blue-100 text-blue-700',
  Signed: 'bg-green-100 text-green-700',
  Cancelled: 'bg-red-100 text-red-700',
}

export const FLAG_COLORS: Record<string, string> = {
  H: 'text-red-600 font-bold',
  L: 'text-blue-600 font-bold',
  N: 'text-green-600',
}

export const PRIORITY_OPTIONS = ['Routine', 'Urgent', 'STAT'] as const
