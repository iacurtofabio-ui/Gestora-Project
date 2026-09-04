import { useEffect, useRef } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useFascePerGiorno } from '@/hooks/useFasceOrarie'
import { useZoneAttive } from '@/hooks/useZone'
import { useCreaPrenotazione, useModificaPrenotazione } from '@/hooks/usePrenotazioni'
import { useAuth } from '@/hooks/useAuth'
import type { PrenotazioneDTO } from '@/types/prenotazione'

const schema = z.object({
    dataPrenotazione: z.string().min(1, 'Data obbligatoria'),
    fasciaOrariaId: z.number().min(1, 'Fascia oraria obbligatoria'),
    zonaId: z.number().nullable().optional(),
    numeroCoperti: z.number().min(1, 'Almeno 1 coperto'),
    note: z.string().optional(),
    nomeCliente: z.string().optional(),
})

type FormValues = z.infer<typeof schema>

type Props = {
    isOpen: boolean
    onClose: () => void
    /**
     * NEW-001 - se valorizzata il modal lavora in modifica: campi precompilati e PUT al posto
     * del POST. Assente (creazione) e' il comportamento storico.
     */
    prenotazione?: PrenotazioneDTO
}

const VALORI_VUOTI: FormValues = {
    dataPrenotazione: '',
    fasciaOrariaId: undefined as unknown as number,
    zonaId: null,
    numeroCoperti: undefined as unknown as number,
    note: '',
    nomeCliente: '',
}

export default function PrenotazioneModal({ isOpen, onClose, prenotazione }: Props) {
    const { user } = useAuth()
    const isStaff = user?.roles.includes('Admin') || user?.roles.includes('Staff')
    const zone = useZoneAttive()
    const creaPrenotazione = useCreaPrenotazione()
    const modificaPrenotazione = useModificaPrenotazione()

    const inModifica = prenotazione !== undefined
    // Le unioni di tavoli sono sempre nella stessa zona (checkpoint 2b), quindi la zona delle
    // postazioni assegnate e' univoca e si presta a precompilare il campo.
    // Lo 0 e' trattato come assenza: nessuna zona ha id 0, e prima che il mapping backend fosse
    // corretto era il valore che arrivava sempre. Meglio ricadere su "nessuna preferenza" che
    // impostare un valore senza opzione corrispondente, che lascia la select senza selezione.
    const zonaAssegnataId = prenotazione?.postazioni[0]?.zonaId || null

    const { register, handleSubmit, reset, setValue, watch, formState: { errors } } = useForm<FormValues>({
        resolver: zodResolver(schema),
        defaultValues: VALORI_VUOTI,
    })

    const dataPrenotazione = watch('dataPrenotazione')
    const giornoSettimana = dataPrenotazione
        ? new Date(`${dataPrenotazione}T00:00:00Z`).getUTCDay()
        : undefined
    const fasceOrarie = useFascePerGiorno(giornoSettimana)

    // Le due select dipendono da liste caricate in modo asincrono: impostarne il valore prima che
    // le <option> esistano lo farebbe cadere a vuoto. Si precompilano quindi in un secondo
    // momento, quando le liste sono arrivate.
    const selezioniDaPrecompilare = useRef(false)

    // Si dipende dall'id e non dall'oggetto: la lista viene rinfrescata da React Query e a ogni
    // refetch l'oggetto cambia identita', il che azzererebbe il form mentre l'utente scrive.
    const prenotazioneId = prenotazione?.id

    useEffect(() => {
        if (!isOpen) {
            reset(VALORI_VUOTI)
            selezioniDaPrecompilare.current = false
            return
        }
        if (prenotazione) {
            reset({
                ...VALORI_VUOTI,
                dataPrenotazione: prenotazione.dataPrenotazione,
                numeroCoperti: prenotazione.numeroCoperti,
                note: prenotazione.note ?? '',
                nomeCliente: prenotazione.nomeCliente ?? '',
            })
            selezioniDaPrecompilare.current = true
        }
        // prenotazione e' volutamente fuori dalle dipendenze: vedi nota su prenotazioneId.
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [isOpen, prenotazioneId, reset])

    useEffect(() => {
        if (!selezioniDaPrecompilare.current || !prenotazione) return
        if (!fasceOrarie.data || !zone.data) return
        setValue('fasciaOrariaId', prenotazione.fasciaOrariaId)
        setValue('zonaId', zonaAssegnataId)
        selezioniDaPrecompilare.current = false
    }, [fasceOrarie.data, zone.data, prenotazione, zonaAssegnataId, setValue])

    // Cambiando data cambiano le fasce disponibili, quindi la scelta precedente non vale piu'.
    // Si azzera solo su un cambio effettivo fra due giorni: al primo popolamento (undefined -> N)
    // non c'e' nulla da azzerare, e azzerare li' cancellerebbe la fascia appena precompilata in
    // modifica.
    const giornoPrecedente = useRef<number | undefined>(undefined)
    useEffect(() => {
        if (giornoPrecedente.current !== undefined && giornoPrecedente.current !== giornoSettimana) {
            setValue('fasciaOrariaId', undefined as unknown as number)
        }
        giornoPrecedente.current = giornoSettimana
    }, [giornoSettimana, setValue])

    const onSubmit = (values: FormValues) => {
        const payload = {
            dataPrenotazione: values.dataPrenotazione,
            fasciaOrariaId: values.fasciaOrariaId,
            zonaId: values.zonaId ?? null,
            numeroCoperti: values.numeroCoperti,
            note: values.note ?? null,
            nomeCliente: isStaff ? (values.nomeCliente || null) : null,
        }

        if (prenotazione) {
            modificaPrenotazione.mutate(
                { id: prenotazione.id, data: payload },
                { onSuccess: onClose }
            )
            return
        }
        creaPrenotazione.mutate(payload, { onSuccess: onClose })
    }

    if (!isOpen) return null

    const inCorso = creaPrenotazione.isPending || modificaPrenotazione.isPending

    return (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
            <div className="bg-white rounded-lg p-6 w-full max-w-md">
                <h2 className="text-lg font-semibold mb-4">
                    {inModifica ? 'Modifica Prenotazione' : 'Nuova Prenotazione'}
                </h2>
                <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-4">

                    <div>
                        <label className="text-sm font-medium">Data</label>
                        <input type="date" {...register('dataPrenotazione')} className="border rounded px-3 py-2 w-full text-sm" />
                        {errors.dataPrenotazione && <p className="text-red-500 text-xs mt-1">{errors.dataPrenotazione.message}</p>}
                    </div>

                    <div>
                        <label className="text-sm font-medium">Fascia Oraria</label>
                        <select
                            {...register('fasciaOrariaId', { valueAsNumber: true })}
                            className="border rounded px-3 py-2 w-full text-sm"
                            disabled={giornoSettimana === undefined}
                        >
                            <option value="">
                                {giornoSettimana === undefined ? '-- Seleziona prima una data --' : '-- Seleziona --'}
                            </option>
                            {fasceOrarie.data?.map((f) => (
                                <option key={f.id} value={f.id}>{f.orarioInizio} - {f.orarioFine}</option>
                            ))}
                        </select>
                        {giornoSettimana !== undefined && fasceOrarie.data?.length === 0 && (
                            <p className="text-gray-400 text-xs mt-1">Nessuna fascia oraria attiva per questo giorno.</p>
                        )}
                        {errors.fasciaOrariaId && <p className="text-red-500 text-xs mt-1">{errors.fasciaOrariaId.message}</p>}
                    </div>

                    <div>
                        <label className="text-sm font-medium">Zona (opzionale)</label>
                        {/* REV-015: il campo era gestito a mano con onChange + setValue e non era
                            registrato nel form. Il valore arrivava al submit, ma la select restava
                            fuori dal controllo di react-hook-form: reset() non la ripuliva, e
                            riaprendo il modal si vedeva ancora la zona scelta prima mentre il form
                            era tornato a "nessuna preferenza". Si prenotava credendo di aver
                            scelto una zona. Ora e' un campo registrato come gli altri. */}
                        <select
                            {...register('zonaId', {
                                // La select restituisce sempre stringhe: la stringa vuota e'
                                // l'assenza di preferenza, che il backend si aspetta come null.
                                setValueAs: (v) => (v === '' || v === null ? null : Number(v)),
                            })}
                            className="border rounded px-3 py-2 w-full text-sm"
                        >
                            <option value="">-- Nessuna preferenza --</option>
                            {zone.data?.map((z) => (
                                <option key={z.id} value={z.id}>{z.nome}</option>
                            ))}
                        </select>
                        {inModifica && (
                            <p className="text-gray-400 text-xs mt-1">
                                Salvando, i tavoli vengono riassegnati in base a questa preferenza.
                            </p>
                        )}
                    </div>

                    {isStaff && (
                        <div>
                            <label className="text-sm font-medium">Nome cliente (facoltativo)</label>
                            <input
                                type="text"
                                placeholder="Es. prenotazione presa telefonicamente"
                                {...register('nomeCliente')}
                                className="border rounded px-3 py-2 w-full text-sm"
                            />
                        </div>
                    )}

                    <div>
                        <label className="text-sm font-medium">Numero Coperti</label>
                        <input type="number" {...register('numeroCoperti', { valueAsNumber: true })} className="border rounded px-3 py-2 w-full text-sm" />
                        {errors.numeroCoperti && <p className="text-red-500 text-xs mt-1">{errors.numeroCoperti.message}</p>}
                    </div>

                    <div>
                        <label className="text-sm font-medium">Note (opzionale)</label>
                        <textarea {...register('note')} className="border rounded px-3 py-2 w-full text-sm" rows={3}
                        />
                    </div>

                    <div className="flex justify-end gap-2">
                        <button type="button" onClick={onClose} className="px-4 py-2 text-sm border rounded">Annulla</button>
                        <button
                            type="submit"
                            disabled={inCorso}
                            className="px-4 py-2 text-sm bg-blue-500 text-white rounded disabled:opacity-50"
                        >
                            {inCorso ? 'Salvataggio...' : 'Salva'}
                        </button>
                    </div>
                </form>
            </div>
        </div>
    )
}
