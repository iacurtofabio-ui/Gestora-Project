export type UserDTO = {
  id: string
  userName: string
  email: string
  roles: string[]
}

export type UpdateUserFormDTO = {
  userName: string
  email: string
}

export type AssignRoleDTO = {
  userId: string
  role: string
}

export type ResetPasswordDTO = {
  newPassword: string
}

export const RUOLI_DISPONIBILI = ['Admin', 'Staff', 'Cliente'] as const
export type RuoloDisponibile = typeof RUOLI_DISPONIBILI[number]