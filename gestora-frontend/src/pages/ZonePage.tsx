import { useState } from 'react'
import { PlusIcon } from 'lucide-react'
import { useDeleteZona, useZone } from '@/hooks/useZone'
import type { ZonaDTO } from '@/types/zona'
import ZonaModal from '@/components/ZonaModal'
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

/** Larghezze reali delle colonne: le usa la tabella e le riusa lo scheletro di caricamento. */
const COLONNE_ADMIN = ['minmax(10rem,1fr)', '10rem', '8rem']
const COLONNE_LETTURA = ['minmax(10rem,1fr)', '10rem']

export default function ZonePage() {
  const { user } = useAuth()
  const isAdmin = user?.roles.includes('Admin')
  const zone = useZone()
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [zonaSelezionata, setZonaSelezionata] = useState<ZonaDTO | undefined>(undefined)
  const deleteZona = useDeleteZona()
  const [zonaDaEliminare, setZonaDaEliminare] = useState<ZonaDTO | undefined>(undefined)

  const colonne = isAdmin ? COLONNE_ADMIN : COLONNE_LETTURA

  function apriNuova() {
    setZonaSelezionata(undefined)
    setIsModalOpen(true)
  }

  function apriModifica(zona: ZonaDTO) {
    setZonaSelezionata(zona)
    setIsModalOpen(true)
  }

  return (
    <div className="mx-auto max-w-4xl space-y-4">
      <IntestazionePagina
        titolo="Zone"
        conteggio={
          zone.data ? `${zone.data.length} ${zone.data.length === 1 ? 'zona' : 'zone'}` : undefined
        }
        azione={
          isAdmin && (
            <Button size="sm" onClick={apriNuova}>
              <PlusIcon />
              Aggiungi zona
            </Button>
          )
        }
      />

      <div className="overflow-hidden rounded-xl border bg-card">
        {/* REV-074: intestazione e pulsante restano a schermo mentre la tabella carica o fallisce. */}
        {zone.isLoading ? (
          <div className="p-3">
            <TableSkeleton righe={4} colonne={colonne} />
          </div>
        ) : zone.isError ? (
          <PageError
            error={zone.error}
            fallback="Errore nel caricamento delle zone."
            onRiprova={() => zone.refetch()}
          />
        ) : (
          <div className="overflow-x-auto">
            <Table style={{ tableLayout: 'fixed', minWidth: '32rem' }}>
              <colgroup>
                {colonne.map((larghezza, i) => (
                  <col key={i} style={{ width: larghezza }} />
                ))}
              </colgroup>
              <TableHeader>
                <TableRow className="hover:bg-transparent">
                  <TableHead className="text-nota font-medium text-muted-foreground">Nome</TableHead>
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
                {zone.data?.length === 0 ? (
                  <EmptyState
                    colSpan={colonne.length}
                    messaggio={
                      isAdmin
                        ? 'La sala non ha ancora zone. Una zona raggruppa i tavoli che stanno nello stesso posto — sala, dehors, sala privata — ed e’ il primo passo prima di aggiungere i tavoli.'
                        : 'La sala non ha ancora zone. Le configura un amministratore.'
                    }
                    azione={
                      isAdmin && (
                        <Button size="sm" onClick={apriNuova}>
                          <PlusIcon />
                          Aggiungi la prima zona
                        </Button>
                      )
                    }
                  />
                ) : (
                  zone.data?.map((zona) => (
                    <TableRow key={zona.id} className="group/riga">
                      <TableCell className="text-corpo truncate font-medium">{zona.nome}</TableCell>
                      <TableCell>
                        <StatoAttivo attiva={zona.attiva} />
                      </TableCell>
                      {isAdmin && (
                        <TableCell>
                          <AzioniRiga
                            descrizione={`la zona ${zona.nome}`}
                            azionePrimaria={{
                              etichetta: 'Modifica',
                              onSelect: () => apriModifica(zona),
                            }}
                            distruttiva={{
                              etichetta: 'Elimina zona',
                              onSelect: () => setZonaDaEliminare(zona),
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

      <ZonaModal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        zona={zonaSelezionata}
      />
      <ConfirmDialog
        open={zonaDaEliminare !== undefined}
        titolo={`Eliminare la zona ${zonaDaEliminare?.nome ?? ''}?`}
        testoConferma="Elimina zona"
        descrizione="Spariscono anche i tavoli che le appartengono, e con loro la possibilita' di assegnarli. Se ti serve solo toglierla dalla rotazione, disattivala invece di eliminarla: resta configurata e la puoi riaccendere."
        onConfirm={() => {
          deleteZona.mutate(zonaDaEliminare!.id)
          setZonaDaEliminare(undefined)
        }}
        onCancel={() => setZonaDaEliminare(undefined)}
      />
    </div>
  )
}
