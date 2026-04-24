export interface UserResponse {
  userId: string
  email: string
  firstName: string
  lastName: string
  roleId: number
  roleName: string
  department: string | null
  phone: string | null
  isActive: boolean
  mustChangePassword: boolean
  createdAt: string
}

export interface CreateUserRequest {
  email: string
  firstName: string
  lastName: string
  roleId: number
  department?: string
  phone?: string
}

export interface UpdateUserRequest {
  firstName?: string
  lastName?: string
  roleId?: number
  department?: string
  phone?: string
}

export interface ResetPasswordRequest {
  newPassword: string
}

export const ROLE_OPTIONS = [
  { id: 1, name: 'SuperAdmin' },
  { id: 2, name: 'Admin' },
  { id: 3, name: 'Doctor' },
  { id: 4, name: 'LabTechnician' },
  { id: 5, name: 'Receptionist' },
  { id: 6, name: 'BillingOfficer' },
] as const

export const ROLE_COLORS: Record<number, string> = {
  1: 'bg-red-100 text-red-700',
  2: 'bg-purple-100 text-purple-700',
  3: 'bg-blue-100 text-blue-700',
  4: 'bg-cyan-100 text-cyan-700',
  5: 'bg-yellow-100 text-yellow-700',
  6: 'bg-green-100 text-green-700',
}
