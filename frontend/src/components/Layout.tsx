import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'

const ADMIN_ROLES = ['SuperAdmin', 'Admin']
const BILLING_ROLES = ['SuperAdmin', 'Admin', 'BillingOfficer', 'Receptionist']

function NavItem({ to, label }: { to: string; label: string }) {
  return (
    <NavLink
      to={to}
      className={({ isActive }) =>
        `block px-3 py-2 rounded-md text-sm font-medium transition-colors ${
          isActive
            ? 'bg-sky-700 text-white'
            : 'text-sky-100 hover:bg-sky-700 hover:text-white'
        }`
      }
    >
      {label}
    </NavLink>
  )
}

export function Layout() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()

  function handleLogout() {
    logout()
    navigate('/login')
  }

  const isAdmin = ADMIN_ROLES.includes(user?.role ?? '')
  const canBill = BILLING_ROLES.includes(user?.role ?? '')
  const isLabTech = user?.role === 'LabTechnician'
  const isDoctor = user?.role === 'Doctor'

  return (
    <div className="min-h-screen flex bg-gray-50">
      {/* Sidebar */}
      <aside className="w-56 bg-sky-800 flex flex-col shrink-0">
        <div className="px-4 py-5 border-b border-sky-700">
          <p className="text-white font-bold text-lg leading-tight">KayCare LIS</p>
          <p className="text-sky-300 text-xs mt-0.5">Laboratory Information</p>
        </div>

        <nav className="flex-1 px-3 py-4 space-y-1 overflow-y-auto">
          <NavItem to="/" label="Dashboard" />
          <NavItem to="/patients" label="Patients" />
          <NavItem to="/appointments" label="Appointments" />

          <p className="px-3 pt-3 pb-1 text-xs font-semibold text-sky-400 uppercase tracking-wider">
            Laboratory
          </p>
          <NavItem to="/lab-orders" label="Lab Orders" />
          <NavItem to="/lab-orders/waiting" label="Waiting List" />
          {(isAdmin || isDoctor || isLabTech) && (
            <NavItem to="/hl7-inbox" label="HL7 Inbox" />
          )}

          <p className="px-3 pt-3 pb-1 text-xs font-semibold text-sky-400 uppercase tracking-wider">
            Radiology
          </p>
          <NavItem to="/radiology" label="Radiology Orders" />

          {canBill && (
            <>
              <p className="px-3 pt-3 pb-1 text-xs font-semibold text-sky-400 uppercase tracking-wider">
                Billing
              </p>
              <NavItem to="/billing" label="Bills" />
            </>
          )}

          {isAdmin && (
            <>
              <p className="px-3 pt-3 pb-1 text-xs font-semibold text-sky-400 uppercase tracking-wider">
                Administration
              </p>
              <NavItem to="/admin/users" label="Staff Users" />
              <NavItem to="/admin/departments" label="Departments" />
              <NavItem to="/admin/settings" label="Facility Settings" />
              <NavItem to="/audit-logs" label="Audit Logs" />
            </>
          )}
        </nav>

        <div className="px-3 py-4 border-t border-sky-700">
          <p className="text-sky-200 text-xs font-medium truncate">
            {user?.firstName} {user?.lastName}
          </p>
          <p className="text-sky-400 text-xs truncate">{user?.role}</p>
          <button
            onClick={handleLogout}
            className="mt-2 w-full text-left text-xs text-sky-300 hover:text-white transition-colors"
          >
            Sign out →
          </button>
        </div>
      </aside>

      {/* Main content */}
      <main className="flex-1 overflow-auto">
        <Outlet />
      </main>
    </div>
  )
}
