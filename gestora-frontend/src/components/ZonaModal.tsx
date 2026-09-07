import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import type { ZonaDTO } from '@/types/zona'
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

  const {
    register,
    handleSubmit,
    formState: { errors },
    reset,
  } = useForm<ZonaForm>({
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

  // REV-044: il pulsante segue lo stato della mutation, non isSubmitting di react-hook-form.
  // Quest'ultimo torna false appena onSubmit ritorna, e onSubmit lancia mutate() senza attenderla:
  // il pulsante si riabilitava mentre la richiesta era ancora in volo, quindi un secondo clic
  // partiva davvero e creava un doppione.
  const inCorso = creaZona.isPending || updateZona.isPending

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
          <div className="space-y-1">
            <Label htmlFor="zona-nome">Nome</Label>
            <Input id="zona-nome" {...register('nome')} type="text" placeholder="Nome" />
            {errors.nome && <p className="text-red-500 text-sm mt-1">{errors.nome.message}</p>}
          </div>
          <div className="flex items-center gap-2">
            <input id="zona-attiva" {...register('attiva')} type="checkbox" className="h-4 w-4" />
            <Label htmlFor="zona-attiva">Attiva</Label>
          </div>
          <Button type="submit" disabled={inCorso}>
            {inCorso ? 'Salvataggio...' : 'Salva'}
          </Button>
        </form>
      </DialogContent>
    </Dialog>
  )
}
