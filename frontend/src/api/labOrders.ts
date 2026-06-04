import { apiClient } from './client'
import type {
  LabTestCatalogItem,
  LabOrderSummary,
  LabOrderDetail,
  CreateLabOrderRequest,
  ManualResultRequest,
  LabOrderItemResponse,
} from '../types/labOrders'

export const getCatalog = (includeInactive = false) =>
  apiClient.get<LabTestCatalogItem[]>('/lab-orders/catalog', { params: { includeInactive } }).then((r) => r.data)

export const createCatalogItem = (data: Omit<LabTestCatalogItem, 'labTestCatalogId'>) =>
  apiClient.post<LabTestCatalogItem>('/lab-orders/catalog', data).then((r) => r.data)

export const updateCatalogItem = (id: string, data: Partial<LabTestCatalogItem> & { isActive?: boolean }) =>
  apiClient.put<LabTestCatalogItem>(`/lab-orders/catalog/${id}`, data).then((r) => r.data)

export const deleteCatalogItem = (id: string) =>
  apiClient.delete(`/lab-orders/catalog/${id}`).then((r) => r.data)

export const getWaitingList = () =>
  apiClient.get<LabOrderSummary[]>('/lab-orders/waiting-list').then((r) => r.data)

export const getLabOrdersForPatient = (patientId: string) =>
  apiClient.get<LabOrderSummary[]>('/lab-orders', { params: { patientId } }).then((r) => r.data)

export const getLabOrder = (id: string) =>
  apiClient.get<LabOrderDetail>(`/lab-orders/${id}`).then((r) => r.data)

export const createLabOrder = (data: CreateLabOrderRequest) =>
  apiClient.post<LabOrderDetail>('/lab-orders', data).then((r) => r.data)

export const receiveSample = (id: string) =>
  apiClient.post(`/lab-orders/${id}/receive-sample`).then((r) => r.data)

export const enterManualResult = (orderId: string, itemId: string, data: ManualResultRequest) =>
  apiClient.post(`/lab-orders/${orderId}/items/${itemId}/result`, data).then((r) => r.data)

export const signItem = (orderId: string, itemId: string) =>
  apiClient.post(`/lab-orders/${orderId}/items/${itemId}/sign`).then((r) => r.data)

export const downloadLabReport = (id: string) =>
  apiClient.get(`/lab-orders/${id}/report`, { responseType: 'blob' }).then((r) => r.data)

export const recordCriticalCallLog = (itemId: string, data: { recipientName: string; notes: string }) =>
  apiClient.post<LabOrderItemResponse>(`/lab-orders/items/${itemId}/critical-log`, data).then((r) => r.data)

export const getCriticalAlerts = () =>
  apiClient.get<LabOrderItemResponse[]>('/lab-orders/critical-alerts').then((r) => r.data)
