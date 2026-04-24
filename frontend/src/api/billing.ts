import { apiClient } from './client'
import type {
  BillSummary,
  BillDetail,
  CreateBillRequest,
  AddPaymentRequest,
  AdjustBillRequest,
} from '../types/billing'

export const getBills = (patientId?: string) =>
  apiClient.get<BillSummary[]>('/bills', { params: { patientId } }).then((r) => r.data)

export const getBill = (id: string) =>
  apiClient.get<BillDetail>(`/bills/${id}`).then((r) => r.data)

export const createBill = (data: CreateBillRequest) =>
  apiClient.post<BillDetail>('/bills', data).then((r) => r.data)

export const issueBill = (id: string) =>
  apiClient.post(`/bills/${id}/issue`).then((r) => r.data)

export const voidBill = (id: string) =>
  apiClient.post(`/bills/${id}/void`).then((r) => r.data)

export const addPayment = (id: string, data: AddPaymentRequest) =>
  apiClient.post(`/bills/${id}/payments`, data).then((r) => r.data)

export const adjustBill = (id: string, data: AdjustBillRequest) =>
  apiClient.post(`/bills/${id}/adjust`, data).then((r) => r.data)

export const downloadInvoice = (id: string) =>
  apiClient.get(`/bills/${id}/report`, { responseType: 'blob' }).then((r) => r.data)

export const downloadReceipt = (paymentId: string) =>
  apiClient.get(`/bills/payments/${paymentId}/receipt`, { responseType: 'blob' }).then((r) => r.data)
