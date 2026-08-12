import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { useEffect } from 'react'
import type { AxiosError } from 'axios'
import type { PostazioneDTO } from "@/types/postazione";
import type { ApiErrorResponse } from '@/types/apiError'
import { useCreaPostazione, useUpdatePostazione } from "@/hooks/usePostazioni";
import { useZone } from "@/hooks/useZone";
import { toast } from "sonner";

const schema = z.object({
    numero: z.number().min(1, 'Numero obbligatorio'),
    capienzaMassima: z.number().min(1, 'Capienza obbligatoria'),
    zonaId: z.number().min(1, 'Zona obbligatoria'),
    attiva: z.boolean(),
})

type PostazioneForm = z.infer<typeof schema>

type Props = {
    isOpen: boolean
    onClose: () => void
    postazione?: PostazioneDTO
}

export default function PostazioneModal({ isOpen, onClose, postazione }: Props) {

    const creaPostazione = useCreaPostazione()
    const updatePostazione = useUpdatePostazione()
    const zone = useZone()

    const { register, handleSubmit, formState: { errors, isSubmitting }, reset, } = useForm<PostazioneForm>({
        resolver: zodResolver(schema),
        defaultValues: {
            numero: postazione?.numero,
            capienzaMassima: postazione?.capienzaMassima,
            zonaId: postazione?.zonaId,
            attiva: postazione?.attiva ?? true,
        },
    })
    useEffect(() => {
        reset({
            numero: postazione?.numero,
            capienzaMassima: postazione?.capienzaMassima,
            zonaId: postazione?.zonaId,
            attiva: postazione?.attiva ?? true,
        })
    }, [postazione, reset])

    function onSubmit(data: PostazioneForm) {
        if (postazione) {
            updatePostazione.mutate({ ...data, id: postazione.id, prenotazioneId: postazione.prenotazioneId },
                {
                    onSuccess: () => {
                        toast.success('Postazione aggiornata con successo')
                        onClose()
                    },
                    onError: (error: AxiosError<ApiErrorResponse>) => {
                        const data = error.response?.data
                        const errors = data?.errors
                        if (errors && errors.length > 0) {
                            errors.forEach(e => toast.error(e.error))
                        } else {
                            toast.error(data?.message ?? 'Errore imprevisto')
                        }
                    }
                })
        } else {
            creaPostazione.mutate(data, {
                onSuccess: () => {
                    toast.success('Postazione creata con successo')
                    onClose()
                },
                onError: (error: AxiosError<ApiErrorResponse>) => {
                    const data = error.response?.data
                    const errors = data?.errors
                    if (errors && errors.length > 0) {
                        errors.forEach(e => toast.error(e.error))
                    } else {
                        toast.error(data?.message ?? 'Errore imprevisto')
                    }
                }
            })
        }
    }
    return (
        <Dialog open={isOpen} onOpenChange={onClose}>
            <DialogContent>
                <DialogHeader>
                    <DialogTitle>{postazione ? 'Modifica Postazione' : 'Nuova Postazione'}</DialogTitle>
                </DialogHeader>
                <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-4">
                    <div>
                        <label className="text-sm font-medium text-gray-700">Numero postazione</label>
                        <input
                            {...register('numero', { valueAsNumber: true })}
                            type="number"
                            placeholder="Numero"
                            className="w-full border rounded px-3 py-2"
                        />
                        {errors.numero && <p className="text-red-500 text-sm mt-1">{errors.numero.message}</p>}
                    </div>
                    <div>
                        <label className="text-sm font-medium text-gray-700">Capienza massima</label>
                        <input
                            {...register('capienzaMassima', { valueAsNumber: true })}
                            type="number"
                            placeholder="Capienza massima"
                            className="w-full border rounded px-3 py-2"
                        />
                        {errors.capienzaMassima && <p className="text-red-500 text-sm mt-1">{errors.capienzaMassima.message}</p>}
                    </div>
                    <div>
                        <label className="text-sm font-medium text-gray-700">Zona</label>
                        <select
                            {...register('zonaId', { valueAsNumber: true })}
                            className="w-full border rounded px-3 py-2"
                        >
                            <option value="">-- Seleziona zona --</option>
                            {zone.data?.map(z => (
                                <option key={z.id} value={z.id}>{z.nome}</option>
                            ))}
                        </select>
                        {errors.zonaId && <p className="text-red-500 text-sm mt-1">{errors.zonaId.message}</p>}
                    </div>
                    <div className="flex items-center gap-2">
                        <input
                            {...register('attiva')}
                            type="checkbox"
                            className="h-4 w-4"
                        />
                        <span>Attiva</span>
                    </div>
                    <button type="submit" className="bg-blue-500 text-white px-3 py-1 rounded" disabled={isSubmitting}>
                        Salva
                    </button>
                </form>
            </DialogContent>
        </Dialog>
    )
}
