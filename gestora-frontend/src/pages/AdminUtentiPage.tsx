import { useState } from 'react'
import { useUtenti, useDeleteUser } from '@/hooks/useAdminUtenti'
import { useAuth } from '@/hooks/useAuth'
import ConfirmDialog from '@/components/ConfirmDialog'
import { Button } from '@/components/ui/button'
import type { UserDTO } from '@/types/utente'
import EditUserModal from '@/components/EditUserModal'
import GestisciRuoliModal from '@/components/GestisciRuoliModal'
import ResetPasswordModal from '@/components/ResetPasswordModal'
import CreateUserModal from '@/components/CreateUserModal'

export default function AdminUtentiPage() {
  const { user } = useAuth()
  const utenti = useUtenti()
  const deleteUser = useDeleteUser()

  const [idDaEliminare, setIdDaEliminare] = useState<string | undefined>(undefined)
  const [utenteSelezionato, setUtenteSelezionato] = useState<UserDTO | undefined>(undefined)
  const [modalAperto, setModalAperto] = useState<'edit' | 'ruoli' | 'password' | 'crea' | undefined>(undefined)

  if (utenti.isLoading) return <div className="p-6">Caricamento...</div>
  if (utenti.isError) return <div className="p-6 text-red-500">Errore nel caricamento utenti</div>

  return (
    <div className="p-6">
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold">Gestione Utenti</h1>
        <Button onClick={() => setModalAperto('crea')}>+ Crea utente</Button>
      </div>

      <table className="w-full text-sm border-collapse">
        <thead>
          <tr className="border-b text-left text-gray-500">
            <th className="py-2 pr-4">Username</th>
            <th className="py-2 pr-4">Email</th>
            <th className="py-2 pr-4">Ruoli</th>
            <th className="py-2">Azioni</th>
          </tr>
        </thead>
        <tbody>
          {utenti.data?.map((u) => (
            <tr key={u.id} className="border-b hover:bg-gray-50">
              <td className="py-2 pr-4">{u.userName}</td>
              <td className="py-2 pr-4">{u.email}</td>
              <td className="py-2 pr-4">{u.roles.join(', ')}</td>
              <td className="py-2 flex gap-2">
                <Button size="sm" variant="outline"
                  onClick={() => { setUtenteSelezionato(u); setModalAperto('edit') }}>
                  Modifica
                </Button>
                <Button size="sm" variant="outline"
                  onClick={() => { setUtenteSelezionato(u); setModalAperto('ruoli') }}>
                  Ruoli
                </Button>
                <Button size="sm" variant="outline"
                  onClick={() => { setUtenteSelezionato(u); setModalAperto('password') }}>
                  Reset Password
                </Button>
                <Button size="sm" variant="destructive"
                  disabled={u.id === user?.id}
                  onClick={() => setIdDaEliminare(u.id)}>
                  Elimina
                </Button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>

      <ConfirmDialog
        open={idDaEliminare !== undefined}
        descrizione="Questa operazione è irreversibile. L'utente verrà eliminato definitivamente."
        onConfirm={() => { deleteUser.mutate(idDaEliminare!); setIdDaEliminare(undefined) }}
        onCancel={() => setIdDaEliminare(undefined)}
      />
      <EditUserModal
        utente={utenteSelezionato}
        open={modalAperto === 'edit'}
        onClose={() => setModalAperto(undefined)}
      />
      <GestisciRuoliModal
        utente={utenteSelezionato}
        open={modalAperto === 'ruoli'}
        onClose={() => setModalAperto(undefined)}
      />
      <ResetPasswordModal
        utente={utenteSelezionato}
        open={modalAperto === 'password'}
        onClose={() => setModalAperto(undefined)}
      />
      <CreateUserModal
        open={modalAperto === 'crea'}
        onClose={() => setModalAperto(undefined)}
      />
    </div>

  )
}