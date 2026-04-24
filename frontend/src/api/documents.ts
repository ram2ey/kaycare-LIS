import { apiClient } from './client'

export interface DocumentResponse {
  documentId: string
  patientId: string
  fileName: string
  contentType: string
  fileSizeBytes: number
  documentType: string | null
  notes: string | null
  uploadedByUserId: string
  uploadedByName: string
  createdAt: string
  downloadUrl: string
}

export const getDocuments = (patientId: string) =>
  apiClient.get<DocumentResponse[]>('/documents', { params: { patientId } }).then((r) => r.data)

export const uploadDocument = (patientId: string, file: File, documentType?: string, notes?: string) => {
  const form = new FormData()
  form.append('file', file)
  form.append('patientId', patientId)
  if (documentType) form.append('documentType', documentType)
  if (notes) form.append('notes', notes)
  return apiClient.post<DocumentResponse>('/documents', form, {
    headers: { 'Content-Type': 'multipart/form-data' },
  }).then((r) => r.data)
}

export const deleteDocument = (id: string) =>
  apiClient.delete(`/documents/${id}`).then((r) => r.data)
