import { useState } from 'react'
import { PlusIcon } from 'lucide-react'
import type { FasciaOrariaDTO } from '@/types/fasciaOraria'
import { useAllFasceOrarie, useDeleteFasciaOraria } from '@/hooks/useFasceOrarie'
import FasciaOrariaModal from '@/components/FasciaOrariaModal'
import ConfirmDialog from '@/components/ConfirmDialog'
import { PageError, TableSkeleton } from '@/components/PageState'
import { EmptyState } from '@/components/EmptyState'
import { StatoAttivo } from '@/components/StatoAttivo'
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
import { useAuth } from '@/hooks/useAuth'
import { GIORNI_SETTIMANA } from '@/lib/giorni'

const COLONNE_ADMIN = ['9rem', 'minmax(8rem,1fr)', '9rem', '8rem', '8rem']
const COLONNE_LETTURA = ['9rem', 'minmax(8rem,1fr)', '9rem', '8rem']

export default function FasciaOrariaPage() {
  const { user } = useAuth()
  const isAdmin = user?.roles.includes('Admin')
  const { data, isLoading, isError, error, refetch } = useAllFasceOrarie()
  const deleteFasciaOraria = useDeleteFasciaOraria()
  const [fasciaDaEliminare, setFasciaDaEliminare] = useState<FasciaOrariaDTO | undefined>(undefined)

  const [isModalOpen, setIsModalOpen] = useState(false)
  const [fasciaSelezionata, setFasciaSelezionata] = useState<FasciaOrariaDTO | undefined>(undefined)

  const colonne = isAdmin ? COLONNE_ADMIN : COLONNE_LETTURA

  function apriNuova() {
    setFasciaSelezionata(undefined)
    setIsModalOpen(true)
  }

  function apriModifica(fascia: FasciaOrariaDTO) {
    setFasciaSelezionata(fascia)
    setIsModalOpen(true)
  }

  /** "19:00–23:00 di venerdi": e' cosi' che si nomina una fascia parlando, non con il suo id. */
  function nomeFascia(f: FasciaOrariaDTO) {
    return `${f.orarioInizio.slice(0, 5)}–${f.orarioFine.slice(0, 5)} di ${GIORNI_SETTIMANA[f.giornoSettimana]}`
  }

  return (
    <div className="mx-auto max-w-4xl space-y-4">
      <IntestazionePagina
        titolo="Fasce orarie"
        conteggio={data ? `${data.length} ${data.length === 1 ? 'fascia' : 'fasce'}` : undefined}
        azione={
          isAdmin && (
            <Button size="sm" onClick={apriNuova}>
              <PlusIcon />
              Aggiungi fascia
            </Button>
          )
        }
      />

      <div className="overflow-hidden rounded-xl border bg-card">
        {isLoading ? (
          <div className="p-3">
            <TableSkeleton righe={6} colonne={colonne} />
          </div>
        ) : isError ? (
          <PageError
            error={error}
            fallback="Errore nel caricamento delle fasce orarie."
            onRiprova={() => refetch()}
          />
        ) : (
          <div className="overflow-x-auto">
            <Table style={{ tableLayout: 'fixed', minWidth: '44rem' }}>
              <colgroup>
                {colonne.map((larghezza, i) => (
                  <col key={i} style={{ width: larghezza }} />
                ))}
              </colgroup>
              <TableHeader>
                <TableRow className="hover:bg-transparent">
                  <TableHead className="text-nota font-medium text-muted-foreground">
                    Orario
                  </TableHead>
                  <TableHead className="text-nota font-medium text-muted-foreground">
                    Giorno
                  </TableHead>
                  <TableHead className="text-nota text-right font-medium text-muted-foreground">
                    Tetto coperti
                  </TableHead>
                  <TableHead className="text-nota font-medium text-muted-foreground">
                    Stato
                  </TableHead>
                  {isAdmin && (
                    <TableHead className="text-nota text-right font-medium text-muted-foreground">
                      Azioni
                    </TableHead>
                  )}
                </TableRow>
              </TableHeader>
              <TableBody>
                {data?.length === 0 ? (
                  <EmptyState
                    colSpan={colonne.length}
                    messaggio={
                      isAdmin
                        ? 'Nessuna fascia oraria. Senza fasce la sala non accetta prenotazioni: e’ la fascia a dire quando si e’ aperti e quanti coperti al massimo si servono in quel turno.'
                        : 'Nessuna fascia oraria configurata. Le imposta un amministratore.'
                    }
                    azione={
                      isAdmin && (
                        <Button size="sm" onClick={apriNuova}>
                          <PlusIcon />
                          Aggiungi la prima fascia
                        </Button>
                      )
                    }
                  />
                ) : (
                  data?.map((fascia) => (
                    <TableRow key={fascia.id} className="group/riga">
                      {/* L'orario e' il dato che si cerca per primo: un gradino sopra il resto. */}
                      <TableCell className="text-orario tabular-nums">
                        {fascia.orarioInizio.slice(0, 5)}–{fascia.orarioFine.slice(0, 5)}
                      </TableCell>
                      <TableCell className="text-corpo truncate">
                        {GIORNI_SETTIMANA[fascia.giornoSettimana]}
                      </TableCell>
                      <TableCell className="text-orario text-right tabular-nums">
                        {fascia.maxCoperti}
                      </TableCell>
                      <TableCell>
                        <StatoAttivo attiva={fascia.attiva} />
                      </TableCell>
                      {isAdmin && (
                        <TableCell>
                          <AzioniRiga
                            descrizione={`la fascia ${nomeFascia(fascia)}`}
                            azionePrimaria={{
                              etichetta: 'Modifica',
                              onSelect: () => apriModifica(fascia),
                            }}
                            distruttiva={{
                              etichetta: 'Elimina fascia',
                              onSelect: () => setFasciaDaEliminare(fascia),
                            }}
                          />
                        </TableCell>
                      )}
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </div>
        )}
      </div>

      <FasciaOrariaModal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        fascia={fasciaSelezionata}
      />
      <ConfirmDialog
        open={fasciaDaEliminare !== undefined}
        titolo="Eliminare questa fascia oraria?"
        testoConferma="Elimina fascia"
        descrizione={
          fasciaDaEliminare
            ? `Il turno delle ${nomeFascia(fasciaDaEliminare)} smette di esistere e da quel momento non si puo' piu' prenotare in quell'orario. Se ti serve solo chiudere per un periodo, disattivala: resta configurata e la puoi riaccendere.`
            : ''
        }
        onConfirm={() => {
          deleteFasciaOraria.mutate(fasciaDaEliminare!.id)
          setFasciaDaEliminare(undefined)
        }}
        onCancel={() => setFasciaDaEliminare(undefined)}
      />
    </div>
  )
}
