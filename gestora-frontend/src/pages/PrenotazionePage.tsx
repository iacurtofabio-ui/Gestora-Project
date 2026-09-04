import { useState } from 'react'
import { isAxiosError } from 'axios'
import {
    usePrenotazioni,
    useConfermaPrenotazione,
    useCompletaPrenotazione,
    useAnnullaPrenotazione,
    useDeletePrenotazione,
} from '@/hooks/usePrenotazioni'
import PrenotazioneModal from '@/components/PrenotazioneModal'
import ConfirmDialog from '@/components/ConfirmDialog'
import { STATI_PRENOTAZIONE, STATO_LABELS, type PrenotazioneDTO } from '@/types/prenotazione'
import { useAuth } from '@/hooks/useAuth'

const OPZIONI_STATO = [
    { value: '', label: 'Tutti gli stati' },
    { value: STATI_PRENOTAZIONE.ATTIVA, label: 'Attiva' },
    { value: STATI_PRENOTAZIONE.IN_CORSO, label: 'Confermata' },
    { value: STATI_PRENOTAZIONE.COMPLETATA, label: 'Completata' },
    { value: STATI_PRENOTAZIONE.ANNULLATA, label: 'Annullata' },
]

export default function PrenotazionePage() {

    const { user } = useAuth()
    const isStaff = user?.roles.includes('Admin') || user?.roles.includes('Staff')
    // NEW-004: l'eliminazione e' riservata all'Admin (l'endpoint e' [Authorize(Roles = Admin)]).
    const isAdmin = user?.roles.includes('Admin')
    const [filtroData, setFiltroData] = useState('')
    const [filtroStato, setFiltroStato] = useState('')
    const [isModalOpen, setIsModalOpen] = useState(false)
    // NEW-001: lo stesso modal serve creazione e modifica. Se questa e' valorizzata il modal si
    // apre precompilato e salva con PUT, altrimenti crea.
    const [prenotazioneDaModificare, setPrenotazioneDaModificare] = useState<PrenotazioneDTO | undefined>(undefined)
    const [idDaAnnullare, setIdDaAnnullare] = useState<number | undefined>(undefined)
    const [idDaEliminare, setIdDaEliminare] = useState<number | undefined>(undefined)

    function apriNuovaPrenotazione() {
        setPrenotazioneDaModificare(undefined)
        setIsModalOpen(true)
    }

    function apriModificaPrenotazione(p: PrenotazioneDTO) {
        setPrenotazioneDaModificare(p)
        setIsModalOpen(true)
    }

    function chiudiModal() {
        setIsModalOpen(false)
        setPrenotazioneDaModificare(undefined)
    }

    const prenotazioni = usePrenotazioni({
        data: filtroData || undefined,
        stato: filtroStato || undefined,
        pageSize: 100,
    })

    const conferma = useConfermaPrenotazione()
    const completa = useCompletaPrenotazione()
    const annulla = useAnnullaPrenotazione()
    const elimina = useDeletePrenotazione()

    if (prenotazioni.isLoading) return <div>Caricamento...</div>
    if (prenotazioni.isError) {
        const status = isAxiosError(prenotazioni.error) ? prenotazioni.error.response?.status : undefined
        if (status === 403) return (
            <div className="p-6 text-sm text-gray-500">
                Non hai i permessi per visualizzare questa sezione. Contatta l'amministratore.
            </div>
        )
        return <div className="p-6 text-sm text-red-500">Errore nel caricamento</div>
    }

    return (
        <div className="bg-white rounded-lg border">
            {/* HEADER */}
            <div className="flex justify-between items-center p-4 border-b gap-4">
                <h2 className="text-sm font-semibold text-gray-700">Prenotazioni</h2>
                <div className="flex gap-3 items-center">
                    {isStaff && (
                        <>
                            <input
                                type="date"
                                className="border rounded px-3 py-1 text-sm"
                                value={filtroData}
                                onChange={(e) => setFiltroData(e.target.value)}
                            />
                            <select
                                className="border rounded px-3 py-1 text-sm"
                                value={filtroStato}
                                onChange={(e) => setFiltroStato(e.target.value)}
                            >
                                {OPZIONI_STATO.map((o) => (
                                    <option key={o.value} value={o.value}>{o.label}</option>
                                ))}
                            </select>
                        </>
                    )}
                    <button
                        className="bg-blue-500 text-white px-3 py-1 rounded text-sm"
                        onClick={apriNuovaPrenotazione}
                    >
                        + Aggiungi
                    </button>
                </div>
            </div>

            {/* TABELLA */}
            <table className="w-full text-sm">
                <thead>
                    <tr className="border-b">
                        <th className="text-left p-3">Data</th>
                        <th className="text-left p-3">Utente</th>
                        <th className="text-left p-3">Orario</th>
                        <th className="text-left p-3">Coperti</th>
                        <th className="text-left p-3">Stato</th>
                        <th className="text-left p-3">Postazioni</th>
                        <th className="text-left p-3">Azioni</th>
                    </tr>
                </thead>
                <tbody>
                    {prenotazioni.data?.map((p) => (
                        <tr key={p.id} className="border-b">
                            <td className="p-3">{p.dataPrenotazione}</td>
                            <td className="p-3">{p.nomeCliente ?? p.nomeUtente}</td>
                            <td className="p-3">{p.oraInizio} - {p.oraFine}</td>
                            <td className="p-3">{p.numeroCoperti}</td>
                            <td className="p-3">{p.stato ? STATO_LABELS[p.stato] ?? p.stato : '—'}</td>
                            <td className="p-3">
                                {p.postazioni.map((pos) => (
                                    <span key={pos.numero} className="text-xs bg-gray-100 px-2 py-1 rounded mr-1">
                                        {pos.numero} ({pos.nomeZona})
                                    </span>
                                ))}
                            </td>
                            <td className="p-3 flex gap-2">
                                {isStaff && p.stato === STATI_PRENOTAZIONE.ATTIVA && (
                                    <button
                                        className="bg-blue-500 text-white px-3 py-1 rounded text-sm"
                                        onClick={() => conferma.mutate(p.id)}
                                    >
                                        Conferma
                                    </button>
                                )}
                                {isStaff && p.stato === STATI_PRENOTAZIONE.IN_CORSO && (
                                    <button
                                        className="bg-green-500 text-white px-3 py-1 rounded text-sm"
                                        onClick={() => completa.mutate(p.id)}
                                    >
                                        Completa
                                    </button>
                                )}
                                {/* NEW-001: la modifica esisteva solo lato backend. Si offre sulle
                                    prenotazioni ancora Attive; per il Cliente il preavviso minimo di
                                    2h e' verificato dal backend e l'eventuale rifiuto arriva come toast. */}
                                {p.stato === STATI_PRENOTAZIONE.ATTIVA && (
                                    <button
                                        className="text-blue-500 hover:underline text-sm"
                                        onClick={() => apriModificaPrenotazione(p)}
                                    >
                                        Modifica
                                    </button>
                                )}
                                {/* RBAC-002: il Cliente può annullare una propria prenotazione (la lista che
                                    vede è già filtrata solo sulle sue), entro il cutoff verificato dal backend —
                                    l'errore oltre soglia arriva come toast dalla mutation. */}
                                {(p.stato === STATI_PRENOTAZIONE.ATTIVA || p.stato === STATI_PRENOTAZIONE.IN_CORSO) &&
                                    (
                                        <button
                                            className="text-red-500 hover:underline text-sm"
                                            onClick={() => setIdDaAnnullare(p.id)}
                                        >
                                            Annulla
                                        </button>
                                    )}
                                {/* NEW-004: l'hook useDeletePrenotazione esisteva ma nessun
                                    componente lo usava, quindi eliminare passava solo da Postman.
                                    Il backend accetta l'eliminazione solo su Attiva o Annullata:
                                    fuori da quegli stati il pulsante non si mostra, invece di far
                                    scoprire il limite con un 409. Eliminare e' definitivo, per
                                    questo resta separato da Annulla, che e' la via normale. */}
                                {isAdmin && (p.stato === STATI_PRENOTAZIONE.ATTIVA || p.stato === STATI_PRENOTAZIONE.ANNULLATA) && (
                                    <button
                                        className="text-gray-500 hover:underline hover:text-red-600 text-sm"
                                        onClick={() => setIdDaEliminare(p.id)}
                                    >
                                        Elimina
                                    </button>
                                )}
                            </td>
                        </tr>
                    ))}
                </tbody>
            </table>

            <PrenotazioneModal
                isOpen={isModalOpen}
                onClose={chiudiModal}
                prenotazione={prenotazioneDaModificare}
            />
            <ConfirmDialog
                open={idDaAnnullare !== undefined}
                titolo="Conferma annullamento"
                testoConferma="Annulla prenotazione"
                descrizione="Sei sicuro di voler annullare questa prenotazione? I tavoli assegnati tornano disponibili."
                onConfirm={() => { annulla.mutate(idDaAnnullare!); setIdDaAnnullare(undefined) }}
                onCancel={() => setIdDaAnnullare(undefined)}
            />
            <ConfirmDialog
                open={idDaEliminare !== undefined}
                descrizione="La prenotazione viene eliminata definitivamente e non sara' piu' recuperabile. Per liberare i tavoli mantenendo lo storico usa invece Annulla."
                onConfirm={() => { elimina.mutate(idDaEliminare!); setIdDaEliminare(undefined) }}
                onCancel={() => setIdDaEliminare(undefined)}
            />
        </div>
    )
}