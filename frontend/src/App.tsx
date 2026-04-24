import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { AuthProvider } from './context/AuthContext'
import { ProtectedRoute } from './components/ProtectedRoute'
import { Layout } from './components/Layout'

import { LoginPage } from './pages/LoginPage'
import { ChangePasswordPage } from './pages/ChangePasswordPage'
import { DashboardPage } from './pages/DashboardPage'
import { AppointmentsPage } from './pages/AppointmentsPage'

import { PatientsPage } from './pages/patients/PatientsPage'
import { RegisterPatientPage } from './pages/patients/RegisterPatientPage'
import { PatientDetailPage } from './pages/patients/PatientDetailPage'

import { LabOrdersPage } from './pages/labOrders/LabOrdersPage'
import { WaitingListPage } from './pages/labOrders/WaitingListPage'
import { NewLabOrderPage } from './pages/labOrders/NewLabOrderPage'
import { LabOrderDetailPage } from './pages/labOrders/LabOrderDetailPage'
import { Hl7InboxPage } from './pages/labOrders/Hl7InboxPage'

import { RadiologyOrdersPage } from './pages/radiology/RadiologyOrdersPage'
import { NewRadiologyOrderPage } from './pages/radiology/NewRadiologyOrderPage'
import { RadiologyOrderDetailPage } from './pages/radiology/RadiologyOrderDetailPage'

import { BillingPage } from './pages/billing/BillingPage'
import { NewBillPage } from './pages/billing/NewBillPage'
import { BillDetailPage } from './pages/billing/BillDetailPage'

import { UsersPage } from './pages/admin/UsersPage'
import { DepartmentsPage } from './pages/admin/DepartmentsPage'
import { FacilitySettingsPage } from './pages/admin/FacilitySettingsPage'
import { AuditLogsPage } from './pages/admin/AuditLogsPage'

const ADMIN = ['SuperAdmin', 'Admin']
const BILLING = ['SuperAdmin', 'Admin', 'BillingOfficer', 'Receptionist']

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/change-password" element={<ChangePasswordPage />} />

          <Route element={<ProtectedRoute><Layout /></ProtectedRoute>}>
            <Route index element={<DashboardPage />} />

            <Route path="patients">
              <Route index element={<PatientsPage />} />
              <Route path="new" element={<RegisterPatientPage />} />
              <Route path=":id" element={<PatientDetailPage />} />
            </Route>

            <Route path="appointments" element={<AppointmentsPage />} />

            <Route path="lab-orders">
              <Route index element={<LabOrdersPage />} />
              <Route path="waiting" element={<WaitingListPage />} />
              <Route path="new" element={<NewLabOrderPage />} />
              <Route path=":id" element={<LabOrderDetailPage />} />
            </Route>

            <Route path="hl7-inbox" element={<Hl7InboxPage />} />

            <Route path="radiology">
              <Route index element={<RadiologyOrdersPage />} />
              <Route path="new" element={<NewRadiologyOrderPage />} />
              <Route path=":id" element={<RadiologyOrderDetailPage />} />
            </Route>

            <Route path="billing">
              <Route index element={<ProtectedRoute roles={BILLING}><BillingPage /></ProtectedRoute>} />
              <Route path="new" element={<ProtectedRoute roles={BILLING}><NewBillPage /></ProtectedRoute>} />
              <Route path=":id" element={<ProtectedRoute roles={BILLING}><BillDetailPage /></ProtectedRoute>} />
            </Route>

            <Route path="admin">
              <Route path="users" element={<ProtectedRoute roles={ADMIN}><UsersPage /></ProtectedRoute>} />
              <Route path="departments" element={<ProtectedRoute roles={ADMIN}><DepartmentsPage /></ProtectedRoute>} />
              <Route path="settings" element={<ProtectedRoute roles={ADMIN}><FacilitySettingsPage /></ProtectedRoute>} />
            </Route>

            <Route path="audit-logs" element={<ProtectedRoute roles={ADMIN}><AuditLogsPage /></ProtectedRoute>} />

            <Route path="*" element={<Navigate to="/" replace />} />
          </Route>
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  )
}
