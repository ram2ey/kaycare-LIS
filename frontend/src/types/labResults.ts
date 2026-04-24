export interface LabResultSummary {
  labResultId: string
  labOrderId: string
  accessionNumber: string
  patientId: string
  patientName: string
  mrn: string
  source: string
  rawHl7Message: string | null
  receivedAt: string
  observations: LabResultObservation[]
}

export interface LabResultObservation {
  labObservationId: string
  testCode: string
  testName: string
  resultValue: string
  unit: string | null
  referenceRange: string | null
  flag: string | null
  observedAt: string
}
