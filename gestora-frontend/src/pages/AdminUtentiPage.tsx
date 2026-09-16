import { useState } from 'react'
import { PlusIcon } from 'lucide-react'
import { useUtenti, useDeleteUser } from '@/hooks/useAdminUtenti'
import { useAuth } from '@/hooks/useAuth'
import ConfirmDialog from '@/components/ConfirmDialog'
import { PageError, TableSkeleton } from '@/components/PageState'
import { EmptyState } from '@/components/EmptyState'
import { AzioniRiga } from '@/components/AzioniRiga'
import { IntestazionePagina } from '@/components/IntestazionePagina'
import { Button } from '@/components/ui/button'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import type { UserDTO } from '@/types/utente'
import EditUserModal from '@/components/EditUserModal'
import GestisciRuoliModal from '@/components/GestisciRuoliModal'
import ResetPasswordModal from '@/components/ResetPasswordModal'
import CreateUserModal from '@/components/CreateUserModal'

const COLONNE = ['minmax(8rem,1fr)', 'minmax(10rem,1.4fr)', 'minmax(9rem,1fr)', '8rem']

export default function AdminUtentiPage() {
  const { user } = useAuth()
  const utenti = useUtenti()
  const deleteUser = useDeleteUser()

  const [utenteDaEliminare, setUtenteDaEliminare] = useState<UserDTO | undefined>(undefined)
  const [utenteSelezionato, setUtenteSelezionato] = useState<UserDTO | undefined>(undefined)
  const [modalAperto, setModalAperto] = useState<
    'edit' | 'ruoli' | 'password' | 'crea' | undefined
  >(undefined)

  function apri(modale: 'edit' | 'ruoli' | 'password', utente: UserDTO) {
    setUtenteSelezionato(utente)
    setModalAperto(modale)
  }

  return (
    <div className="mx-auto max-w-5xl space-y-4">
      <IntestazionePagina
        titolo="Utenti"
        conteggio={
          utenti.data
            ? `${utenti.data.length} ${utenti.data.length === 1 ? 'utente' : 'utenti'}`
            : undefined
        }
        azione={
          <Button size="sm" onClick={() => setModalAperto('crea')}>
            <PlusIcon />
            Crea utente
          </Button>
        }
      />

      <div className="overflow-hidden rounded-xl border bg-card">
        {/* REV-074: titolo e pulsante restano visibili durante il caricamento. */}
        {utenti.isLoading ? (
          <div className="p-3">
            <TableSkeleton righe={6} colonne={COLONNE} />
          </div>
        ) : utenti.isError ? (
          <PageError
            error={utenti.error}
            fallback="Errore nel caricamento degli utenti."
            onRiprova={() => utenti.refetch()}
          />
        ) : (
          <div className="overflow-x-auto">
            <Table style={{ tableLayout: 'fixed', minWidth: '44rem' }}>
              <colgroup>
                {COLONNE.map((larghezza, i) => (
                  <col key={i} style={{ width: larghezza }} />
                ))}
              </colgroup>
              <TableHeader>
                <TableRow className="hover:bg-transparent">
                  <TableHead className="text-nota font-medium text-muted-foreground">Nome</TableHead>
                  <TableHead className="text-nota font-medium text-muted-foreground">
                    Email
                  </TableHead>
                  <TableHead className="text-nota font-medium text-muted-foreground">
                    Ruoli
                  </TableHead>
                  <TableHead className="text-nota text-right font-medium text-muted-foreground">
                    Azioni
                  </TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {utenti.data?.length === 0 ? (
                  <EmptyState
                    colSpan={COLONNE.length}
                    messaggio="Non c’e’ ancora nessun utente oltre a te. Crea un profilo Staff per chi prende le prenotazioni in sala."
                    azione={
                      <Button size="sm" onClick={() => setModalAperto('crea')}>
                        <PlusIcon />
                        Crea il primo utente
                      </Button>
                    }
                  />
                ) : (
                  utenti.data?.map((u) => {
                    // Un Admin non puo' eliminare se stesso: resterebbe un'installazione senza
                    // nessuno che possa configurarla.
                    const seStesso = u.id === user?.id
                    return (
                      <TableRow key={u.id} className="group/riga">
                        <TableCell className="text-corpo truncate font-medium">
                          {u.userName}
                          {seStesso && (
                            <span className="text-nota text-muted-foreground"> · sei tu</span>
                          )}
                        </TableCell>
                        <TableCell className="text-corpo truncate text-muted-foreground">
                          {u.email}
                        </TableCell>
                        {/* I ruoli erano pillole grigie affiancate: in una tabella pesavano come
                            pulsanti. Sono un dato, quindi si leggono come testo. */}
                        <TableCell className="text-corpo truncate">
                          {u.roles.length === 0 ? (
                            <span className="text-muted-foreground">nessun ruolo</span>
                          ) : (
                            u.roles.join(', ')
                          )}
                        </TableCell>
                        <TableCell>
                          <AzioniRiga
                            descrizione={u.userName ?? u.email ?? 'questo utente'}
                            azionePrimaria={{
                              etichetta: 'Modifica',
                              onSelect: () => apri('edit', u),
                            }}
                            voci={[
                              { etichetta: 'Gestisci i ruoli', onSelect: () => apri('ruoli', u) },
                              {
                                etichetta: 'Reimposta la password',
                                onSelect: () => apri('password', u),
                              },
                            ]}
                            distruttiva={{
                              etichetta: 'Elimina utente',
                              onSelect: () => setUtenteDaEliminare(u),
                              disabilitata: seStesso,
                            }}
                          />
                        </TableCell>
                      </TableRow>
                    )
                  })
                )}
              </TableBody>
            </Table>
          </div>
        )}
      </div>

      <ConfirmDialog
        open={utenteDaEliminare !== undefined}
        titolo={`Eliminare ${utenteDaEliminare?.userName ?? "l'utente"}?`}
        testoConferma="Elimina utente"
        descrizione="L'accesso viene revocato subito e il profilo non si puo' recuperare. Le prenotazioni che ha preso restano nello storico."
        onConfirm={() => {
          deleteUser.mutate(utenteDaEliminare!.id)
          setUtenteDaEliminare(undefined)
        }}
        onCancel={() => setUtenteDaEliminare(undefined)}
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
      <CreateUserModal open={modalAperto === 'crea'} onClose={() => setModalAperto(undefined)} />
    </div>
  )
}
