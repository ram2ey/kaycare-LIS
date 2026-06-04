import { describe, it, expect, vi } from 'vitest'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { PatientTimeline } from './PatientTimeline'
import * as labApi from '../api/labOrders'

// Mock the API modules
vi.mock('../api/labOrders', () => ({
  getLabOrder: vi.fn(),
}))

vi.mock('../api/radiology', () => ({
  getRadiologyOrder: vi.fn(),
}))

const mockAppointments = [
  {
    appointmentId: 'apt-1',
    scheduledAt: '2026-06-03T10:00:00Z',
    doctorName: 'Smith',
    status: 'Scheduled',
    reason: 'Routine checkup',
  },
]

const mockBills = [
  {
    billId: 'bill-1',
    billNumber: 'INV-001',
    totalAmount: 150.0,
    balanceDue: 50.0,
    status: 'PartiallyPaid',
    createdAt: '2026-06-03T11:00:00Z',
  },
]

const mockLabOrders = [
  {
    labOrderId: 'lab-1',
    orderedAt: '2026-06-03T09:00:00Z',
    accessionNumber: 'ACC-2026-00001',
    status: 'Active',
    priority: 'Routine',
    orderingDoctorName: 'House',
    testNames: ['Hemoglobin'],
  },
]

const mockRadiologyOrders = [
  {
    radiologyOrderId: 'rad-1',
    orderedAt: '2026-06-03T12:00:00Z',
    status: 'Scheduled',
    priority: 'STAT',
    orderingDoctorName: 'Strange',
    procedureNames: ['Chest X-Ray'],
  },
]

describe('PatientTimeline Component', () => {
  it('should render all timeline events sorted chronologically (newest first)', () => {
    render(
      <PatientTimeline
        patientName="Jane Doe"
        patientMrn="MRN-12345"
        appointments={mockAppointments as any}
        bills={mockBills as any}
        labOrders={mockLabOrders as any}
        radiologyOrders={mockRadiologyOrders as any}
      />
    )

    // Check headings
    expect(screen.getByText('Invoice Issued (INV-001)')).toBeTruthy()
    expect(screen.getByText('Appointment with Dr. Smith')).toBeTruthy()
    expect(screen.getByText('Lab Order (ACC-2026-00001)')).toBeTruthy()
    expect(screen.getByText('Radiology Order (Chest X-Ray)')).toBeTruthy()

    // Find the sequence of cards rendered
    // Radiology Order (12:00) -> Invoice (11:00) -> Appointment (10:00) -> Lab (09:00)
    const titles = screen.getAllByRole('heading', { level: 3 }).map(el => el.textContent)
    expect(titles[0]).toBe('Radiology Order (Chest X-Ray)')
    expect(titles[1]).toBe('Invoice Issued (INV-001)')
    expect(titles[2]).toBe('Appointment with Dr. Smith')
    expect(titles[3]).toBe('Lab Order (ACC-2026-00001)')
  })

  it('should call getLabOrder when expanding a Lab Order event', async () => {
    const mockDetail = {
      labOrderId: 'lab-1',
      accessionNumber: 'ACC-2026-00001',
      clinicalNotes: 'Test notes',
      items: [
        {
          labOrderItemId: 'item-1',
          testName: 'Hemoglobin',
          testCode: 'HB',
          status: 'Resulted',
          manualResultValue: '14.2',
          manualResultUnit: 'g/dL',
          manualResultReferenceRange: '12.0-16.0',
          manualResultFlag: 'N',
        },
      ],
    }

    vi.mocked(labApi.getLabOrder).mockResolvedValue(mockDetail as any)

    render(
      <PatientTimeline
        patientName="Jane Doe"
        patientMrn="MRN-12345"
        appointments={[]}
        bills={[]}
        labOrders={mockLabOrders as any}
        radiologyOrders={[]}
      />
    )

    const expandBtn = screen.getByText('▼ Expand Diagnostic Details')
    expect(expandBtn).toBeTruthy()

    fireEvent.click(expandBtn)

    // Verify loading states and details call
    expect(labApi.getLabOrder).toHaveBeenCalledWith('lab-1')

    await waitFor(() => {
      expect(screen.getByText('Test notes')).toBeTruthy()
      expect(screen.getByText('14.2')).toBeTruthy()
      expect(screen.getByText('12.0-16.0')).toBeTruthy()
    })
  })
})
