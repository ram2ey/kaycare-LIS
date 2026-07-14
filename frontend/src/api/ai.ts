import { apiClient } from './client';

export interface LabInterpreterResponse {
  interpretation: string;
}

export interface RadiologyDraftResponse {
  findings: string;
  impression: string;
  recommendations: string;
}

export interface Icd10Recommendation {
  code: string;
  description: string;
  matchConfidence: string;
}

export interface Hl7RepairResponse {
  explanation: string;
  repairedPayload: string;
}

export const getLabInterpreter = (patientName: string, testName: string, results: Array<{ testCode: string; testName: string; value: string; unit: string; refRange: string; flag: string }>) =>
  apiClient.post<LabInterpreterResponse>('/ai/lab-interpreter', { patientName, testName, results }).then((r) => r.data);

export const getRadiologyDraft = (itemId: string) =>
  apiClient.post<RadiologyDraftResponse>(`/ai/radiology-drafter/${itemId}`).then((r) => r.data);

export const findIcd10Codes = (query: string) =>
  apiClient.get<Icd10Recommendation[]>('/ai/icd10-finder', { params: { query } }).then((r) => r.data);

export const getPatientSummary = (reportText: string) =>
  apiClient.post<{ summary: string }>('/ai/patient-summary', { reportText }).then((r) => r.data);

export const repairHl7Message = (rawHl7: string) =>
  apiClient.post<Hl7RepairResponse>('/ai/hl7-repair', { rawHl7 }).then((r) => r.data);
