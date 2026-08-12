import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import type { ZonaDTO } from "@/types/zona";
import { useCreaZona, useUpdateZona } from '@/hooks/useZone'
import { useEffect } from 'react'

const schema = z.object({
    nome: z.string().min(1, 'Nome obbligatorio'),
    attiva: z.boolean(),
})

type ZonaForm = z.infer<typeof schema>

type Props = {
    isOpen: boolean
    onClose: () => void
    zona?: ZonaDTO
}

export default function ZonaModal({ isOpen, onClose, zona }: Props) {

    const creaZona = useCreaZona()
    const updateZona = useUpdateZona()

    const { register, handleSubmit, formState: { errors, isSubmitting }, reset, } = useForm<ZonaForm>({
        resolver: zodResolver(schema),
        defaultValues: {
            nome: zona?.nome ?? '',
            attiva: zona?.attiva ?? true,
        },
    })

    useEffect(() => {
        reset({
            nome: zona?.nome ?? '',
            attiva: zona?.attiva ?? true,
        })
    }, [zona, reset])

    function onSubmit(data: ZonaForm) {
        if (zona) {
            updateZona.mutate({ ...data, id: zona.id }, { onSuccess: () => onClose() })
        } else {
            creaZona.mutate(data, { onSuccess: () => onClose() })
        }
    }

    return (
        <Dialog open={isOpen} onOpenChange={onClose}>
            <DialogContent>
                <DialogHeader>
                    <DialogTitle>{zona ? 'Modifica Zona' : 'Nuova Zona'}</DialogTitle>
                </DialogHeader>
                <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-4">
                    <div>
                        <input
                            {...register('nome')}
                            type="text"
                            placeholder="Nome"
                            className="w-full border rounded px-3 py-2"
                        />
                        {errors.nome && <p className="text-red-500 text-sm mt-1">{errors.nome.message}</p>}
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
