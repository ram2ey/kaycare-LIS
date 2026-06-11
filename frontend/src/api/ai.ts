import apiClient from './client';

export interface LabInterpreterResponse {
  interpretation: string;
}

export const getLabInterpreter = (patientName: string, testName: string, results: Array<{ testCode: string; testName: string; value: string; unit: string; refRange: string; flag: string }>) =>
  apiClient.post<LabInterpreterResponse>('/ai/lab-interpreter', { patientName, testName, results }).then((r) => r.data);
