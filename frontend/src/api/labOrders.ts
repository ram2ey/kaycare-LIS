import { apiClient } from './client'
import type {
  LabTestCatalogItem,
  LabOrderSummary,
  LabOrderDetail,
  CreateLabOrderRequest,
  ManualResultRequest,
} from '../types/labOrders'

export const getCatalog = () =>
  apiClient.get<LabTestCatalogItem[]>('/lab-orders/catalog').then((r) => r.data)

export const getWaitingList = () =>
  apiClient.get<LabOrderSummary[]>('/lab-orders/waiting').then((r) => r.data)

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
