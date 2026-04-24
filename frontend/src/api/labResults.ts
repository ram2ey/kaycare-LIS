import { apiClient } from './client'
import type { LabResultSummary } from '../types/labResults'

export const getResultsForPatient = (patientId: string) =>
  apiClient.get<LabResultSummary[]>('/lab-results', { params: { patientId } }).then((r) => r.data)

export const getResultByAccession = (accession: string) =>
  apiClient.get<LabResultSummary>(`/lab-results/accession/${accession}`).then((r) => r.data)

export const getResult = (id: string) =>
  apiClient.get<LabResultSummary>(`/lab-results/${id}`).then((r) => r.data)
