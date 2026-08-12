import { useState } from 'react'
import { isAxiosError } from 'axios'
import {
    usePrenotazioni,
    useConfermaPrenotazione,
    useCompletaPrenotazione,
    useAnnullaPrenotazione,
} from '@/hooks/usePrenotazioni'
import PrenotazioneModal from '@/components/PrenotazioneModal'
import ConfirmDialog from '@/components/ConfirmDialog'
import { STATI_PRENOTAZIONE } from '@/types/prenotazione'
import { useAuth } from '@/hooks/useAuth'

const OPZIONI_STATO = [
    { value: '', label: 'Tutti gli stati' },
    { value: STATI_PRENOTAZIONE.ATTIVA, label: 'Attiva' },
    { value: STATI_PRENOTAZIONE.IN_CORSO, label: 'In Corso' },
    { value: STATI_PRENOTAZIONE.COMPLETATA, label: 'Completata' },
    { value: STATI_PRENOTAZIONE.ANNULLATA, label: 'Annullata' },
]

export default function PrenotazionePage() {

    const { user } = useAuth()
    const isStaff = user?.role === 'Admin' || user?.role === 'Staff'
    const [filtroData, setFiltroData] = useState('')
    const [filtroStato, setFiltroStato] = useState('')
    const [isModalOpen, setIsModalOpen] = useState(false)
    const [idDaAnnullare, setIdDaAnnullare] = useState<number | undefined>(undefined)

    const prenotazioni = usePrenotazioni({
        data: filtroData || undefined,
        stato: filtroStato || undefined,
        pageSize: 100,
    })

    const conferma = useConfermaPrenotazione()
    const completa = useCompletaPrenotazione()
    const annulla = useAnnullaPrenotazione()

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
                    <button
                        className="bg-blue-500 text-white px-3 py-1 rounded text-sm"
                        onClick={() => setIsModalOpen(true)}
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
                            <td className="p-3">{p.nomeUtente}</td>
                            <td className="p-3">{p.oraInizio} - {p.oraFine}</td>
                            <td className="p-3">{p.numeroCoperti}</td>
                            <td className="p-3">{p.stato}</td>
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
                                {(p.stato === STATI_PRENOTAZIONE.ATTIVA || p.stato === STATI_PRENOTAZIONE.IN_CORSO) &&
                                    (
                                        <button
                                            className="text-red-500 hover:underline text-sm"
                                            onClick={() => setIdDaAnnullare(p.id)}
                                        >
                                            Annulla
                                        </button>
                                    )}
                            </td>
                        </tr>
                    ))}
                </tbody>
            </table>

            <PrenotazioneModal
                isOpen={isModalOpen}
                onClose={() => setIsModalOpen(false)}
            />
            <ConfirmDialog
                open={idDaAnnullare !== undefined}
                descrizione="Sei sicuro di voler annullare questa prenotazione?"
                onConfirm={() => { annulla.mutate(idDaAnnullare!); setIdDaAnnullare(undefined) }}
                onCancel={() => setIdDaAnnullare(undefined)}
            />
        </div>
    )
}