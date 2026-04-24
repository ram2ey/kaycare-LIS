export interface BillSummary {
  billId: string
  billNumber: string
  patientId: string
  patientName: string
  mrn: string
  status: string
  totalAmount: number
  discountAmount: number
  adjustmentTotal: number
  paidAmount: number
  balanceDue: number
  notes: string | null
  createdAt: string
}

export interface BillDetail extends BillSummary {
  consultationId: string | null
  items: BillItemResponse[]
  payments: PaymentResponse[]
}

export interface BillItemResponse {
  billItemId: string
  description: string
  category: string | null
  quantity: number
  unitPrice: number
  totalPrice: number
}

export interface PaymentResponse {
  paymentId: string
  amount: number
  paymentMethod: string
  reference: string | null
  notes: string | null
  recordedByUserId: string
  recordedByName: string
  paidAt: string
}

export interface CreateBillRequest {
  patientId: string
  consultationId?: string
  notes?: string
  items: BillItemRequest[]
}

export interface BillItemRequest {
  description: string
  category?: string
  quantity: number
  unitPrice: number
}

export interface AddPaymentRequest {
  amount: number
  paymentMethod: string
  reference?: string
  notes?: string
}

export interface AdjustBillRequest {
  adjustmentAmount: number
  reason: string
}

export const BILL_STATUS_COLORS: Record<string, string> = {
  Draft: 'bg-gray-100 text-gray-600',
  Issued: 'bg-blue-100 text-blue-700',
  PartiallyPaid: 'bg-yellow-100 text-yellow-700',
  Paid: 'bg-green-100 text-green-700',
  Voided: 'bg-red-100 text-red-700',
  Waived: 'bg-purple-100 text-purple-700',
}

export const PAYMENT_METHODS = ['Cash', 'MoMo', 'Card', 'BankTransfer', 'NHIS', 'Insurance', 'Cheque'] as const
