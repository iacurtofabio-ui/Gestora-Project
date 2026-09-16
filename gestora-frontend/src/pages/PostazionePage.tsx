import { useState } from 'react'
import { Link } from 'react-router-dom'
import { PlusIcon } from 'lucide-react'
import { useZone } from '@/hooks/useZone'
import { usePostazioni, useDeletePostazione, useRiepilogoSala } from '@/hooks/usePostazioni'
import type { PostazioneDTO } from '@/types/postazione'
import PostazioneModal from '@/components/PostazioneModal'
import ConfirmDialog from '@/components/ConfirmDialog'
import { PageError, TableSkeleton } from '@/components/PageState'
import { EmptyState } from '@/components/EmptyState'
import { StatoAttivo } from '@/components/StatoAttivo'
import { AzioniRiga } from '@/components/AzioniRiga'
import { IntestazionePagina } from '@/components/IntestazionePagina'
import { Button } from '@/components/ui/button'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { useAuth } from '@/hooks/useAuth'
import { cn } from '@/lib/utils'

const COLONNE_ADMIN = ['8rem', '8rem', 'minmax(8rem,1fr)', '8rem']
const COLONNE_LETTURA = ['8rem', '8rem', 'minmax(8rem,1fr)']

export default function PostazionePage() {
  const { user } = useAuth()
  const isAdmin = user?.roles.includes('Admin')
  const isStaffOrAdmin = isAdmin || user?.roles.includes('Staff')
  const riepilogo = useRiepilogoSala({ enabled: !!isStaffOrAdmin })
  const zone = useZone()
  const [zonaSelezionataId, setZonaSelezionataId] = useState<number | undefined>(undefined)
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [postazioneSelezionata, setPostazioneSelezionata] = useState<PostazioneDTO | undefined>(
    undefined
  )
  const deletePostazione = useDeletePostazione()
  const postazioni = usePostazioni(zonaSelezionataId ?? 0, {
    enabled: zonaSelezionataId !== undefined,
  })
  const [postazioneDaEliminare, setPostazioneDaEliminare] = useState<PostazioneDTO | undefined>(
    undefined
  )

  const colonne = isAdmin ? COLONNE_ADMIN : COLONNE_LETTURA
  const zonaSelezionata = zone.data?.find((z) => z.id === zonaSelezionataId)

  if (zone.isLoading)
    return (
      <div className="mx-auto max-w-4xl rounded-xl border bg-card p-3">
        <TableSkeleton righe={4} colonne={colonne} />
      </div>
    )
  if (zone.isError)
    return (
      <PageError
        error={zone.error}
        fallback="Errore nel caricamento delle zone."
        onRiprova={() => zone.refetch()}
        className="mx-auto max-w-4xl"
      />
    )

  function apriNuova() {
    setPostazioneSelezionata(undefined)
    setIsModalOpen(true)
  }

  function apriModifica(postazione: PostazioneDTO) {
    setPostazioneSelezionata(postazione)
    setIsModalOpen(true)
  }

  return (
    <div className="mx-auto max-w-4xl space-y-8">
      <IntestazionePagina
        titolo="Tavoli"
        conteggio={
          riepilogo.data
            ? `${riepilogo.data.tavoliAttivi} attivi · ${riepilogo.data.postiTotali} posti`
            : undefined
        }
        azione={
          isAdmin && (
            <Button size="sm" onClick={apriNuova} disabled={zonaSelezionataId === undefined}>
              <PlusIcon />
              Aggiungi tavolo
            </Button>
          )
        }
      />

      {/* ------------------------------------------------------------------
          Decisione 9: il riepilogo della sala sta in cima ed e' SOLO informativo.
          Nella direzione «Turno» diventa un elenco di righe con la copertura del tetto, non una
          tabella dentro una card: e' un contesto, non un dato su cui si agisce.
          ------------------------------------------------------------------ */}
      {isStaffOrAdmin && riepilogo.data && riepilogo.data.fasce.length > 0 && (
        <section className="space-y-1">
          <h2 className="text-sezione text-muted-foreground">
            I tavoli bastano a coprire il tetto?
          </h2>
          <ul>
            {riepilogo.data.fasce.map((f) => (
              <li
                key={f.fasciaOrariaId}
                className="grid grid-cols-[auto_1fr] items-baseline gap-x-4 gap-y-1 border-b py-2.5 last:border-b-0 sm:grid-cols-[7rem_9rem_1fr]"
              >
                <span className="text-corpo capitalize">{f.giornoSettimana}</span>
                <span className="text-orario tabular-nums whitespace-nowrap">
                  {f.orarioInizio.slice(0, 5)}–{f.orarioFine.slice(0, 5)}
                </span>
                {/* Il colore da solo non basta: chi non distingue le tinte deve comunque leggere
                    se il tetto e' coperto oppure no. */}
                <span
                  className={cn(
                    'text-corpo col-span-2 tabular-nums sm:col-span-1',
                    f.tettoCoperto ? 'text-muted-foreground' : 'text-warning'
                  )}
                >
                  {f.postiTavoli} posti su {f.maxCoperti} di tetto
                  {f.tettoCoperto ? '' : ' — i tavoli non bastano'}
                </span>
              </li>
            ))}
          </ul>
        </section>
      )}

      <div className="space-y-3">
        <div className="flex flex-wrap items-center gap-2">
          <Select
            value={zonaSelezionataId !== undefined ? String(zonaSelezionataId) : undefined}
            onValueChange={(v) => setZonaSelezionataId(Number(v))}
          >
            <SelectTrigger className="h-8 w-[200px]" aria-label="Scegli la zona">
              <SelectValue placeholder="Scegli una zona" />
            </SelectTrigger>
            <SelectContent>
              {zone.data?.map((zona) => (
                <SelectItem key={zona.id} value={String(zona.id)}>
                  {zona.nome}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        <div className="overflow-hidden rounded-xl border bg-card">
          {/* REV-073: prima di scegliere una zona la tabella era vuota e sembrava un errore, non
              uno stato d'attesa. Se poi non c'e' nemmeno una zona, il problema e' un altro e va
              detto: i tavoli stanno dentro una zona, quindi la zona viene prima. */}
          {zone.data?.length === 0 ? (
            <div className="p-8 text-center">
              <p className="text-sezione">Prima servono le zone</p>
              <p className="text-corpo mx-auto mt-1 mb-4 max-w-sm text-muted-foreground text-pretty">
                Ogni tavolo sta dentro una zona, quindi la sala si configura da li'.
              </p>
              {isAdmin && (
                <Button asChild size="sm">
                  <Link to="/zone">Vai alle zone</Link>
                </Button>
              )}
            </div>
          ) : zonaSelezionataId === undefined ? (
            <div className="p-8 text-center">
              <p className="text-corpo mx-auto max-w-sm text-muted-foreground text-pretty">
                Scegli una zona qui sopra per vedere i suoi tavoli.
              </p>
            </div>
          ) : postazioni.isLoading ? (
            <div className="p-3">
              <TableSkeleton righe={5} colonne={colonne} />
            </div>
          ) : postazioni.isError ? (
            <PageError
              error={postazioni.error}
              fallback="Errore nel caricamento dei tavoli."
              onRiprova={() => postazioni.refetch()}
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
                    <TableHead className="text-nota font-medium text-muted-foreground">
                      Tavolo
                    </TableHead>
                    <TableHead className="text-nota text-right font-medium text-muted-foreground">
                      Posti
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
                  {postazioni.data?.length === 0 ? (
                    <EmptyState
                      colSpan={colonne.length}
                      messaggio={`In ${zonaSelezionata?.nome ?? 'questa zona'} non c’e’ ancora nessun tavolo. Finche’ non ce n’e’ almeno uno, in questa zona non si puo’ sedere nessuno.`}
                      azione={
                        isAdmin && (
                          <Button size="sm" onClick={apriNuova}>
                            <PlusIcon />
                            Aggiungi il primo tavolo
                          </Button>
                        )
                      }
                    />
                  ) : (
                    postazioni.data?.map((postazione) => (
                      <TableRow key={postazione.id} className="group/riga">
                        <TableCell className="text-orario tabular-nums">
                          {postazione.numero}
                        </TableCell>
                        <TableCell className="text-orario text-right tabular-nums">
                          {postazione.capienzaMassima}
                        </TableCell>
                        <TableCell>
                          <StatoAttivo attiva={postazione.attiva} />
                        </TableCell>
                        {isAdmin && (
                          <TableCell>
                            <AzioniRiga
                              descrizione={`il tavolo ${postazione.numero}`}
                              azionePrimaria={{
                                etichetta: 'Modifica',
                                onSelect: () => apriModifica(postazione),
                              }}
                              distruttiva={{
                                etichetta: 'Elimina tavolo',
                                onSelect: () => setPostazioneDaEliminare(postazione),
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
      </div>

      <PostazioneModal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        postazione={postazioneSelezionata}
      />
      <ConfirmDialog
        open={postazioneDaEliminare !== undefined}
        titolo={`Eliminare il tavolo ${postazioneDaEliminare?.numero ?? ''}?`}
        testoConferma="Elimina tavolo"
        descrizione="La sala perde questi posti e il tetto della fascia potrebbe non essere piu' coperto. Se il tavolo e' solo temporaneamente fuori uso, disattivalo: resta configurato e non viene assegnato."
        onConfirm={() => {
          deletePostazione.mutate(postazioneDaEliminare!.id)
          setPostazioneDaEliminare(undefined)
        }}
        onCancel={() => setPostazioneDaEliminare(undefined)}
      />
    </div>
  )
}
