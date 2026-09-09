import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { NativeSelect } from '@/components/ui/native-select'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { useEffect } from 'react'
import type { PostazioneDTO } from '@/types/postazione'
import { useCreaPostazione, useUpdatePostazione } from '@/hooks/usePostazioni'
import { useZone } from '@/hooks/useZone'

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

  const {
    register,
    handleSubmit,
    formState: { errors },
    reset,
  } = useForm<PostazioneForm>({
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

  // REV-044: il pulsante segue lo stato della mutation, non isSubmitting di react-hook-form.
  // Quest'ultimo torna false appena onSubmit ritorna, e onSubmit lancia mutate() senza attenderla:
  // il pulsante si riabilitava mentre la richiesta era ancora in volo, quindi un secondo clic
  // partiva davvero e creava un doppione.
  const inCorso = creaPostazione.isPending || updatePostazione.isPending

  // REV-042: qui restava solo la chiusura del modal. Il messaggio di esito - riuscito o fallito -
  // lo emette gia' l'hook (usePostazioni), quindi ripeterlo anche qui mostrava due toast per ogni
  // singolo salvataggio: uno dal componente e uno dalla mutation. Era l'unica pagina a farlo.
  function onSubmit(data: PostazioneForm) {
    if (postazione) {
      updatePostazione.mutate(
        { ...data, id: postazione.id, prenotazioneId: postazione.prenotazioneId },
        { onSuccess: () => onClose() }
      )
    } else {
      creaPostazione.mutate(data, { onSuccess: () => onClose() })
    }
  }
  return (
    <Dialog open={isOpen} onOpenChange={onClose}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle className="text-titolo">{postazione ? 'Modifica tavolo' : 'Nuovo tavolo'}</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-4">
          <div className="space-y-1">
            <Label htmlFor="postazione-numero">Numero del tavolo</Label>
            <Input
              id="postazione-numero"
              {...register('numero', { valueAsNumber: true })}
              type="number"
              placeholder="Es. 12"
            />
            {errors.numero && (
              <p className="text-nota text-destructive">{errors.numero.message}</p>
            )}
          </div>
          <div className="space-y-1">
            <Label htmlFor="postazione-capienza">Posti a sedere</Label>
            <Input
              id="postazione-capienza"
              {...register('capienzaMassima', { valueAsNumber: true })}
              type="number"
              placeholder="Es. 4"
            />
            {errors.capienzaMassima && (
              <p className="text-nota text-destructive">{errors.capienzaMassima.message}</p>
            )}
          </div>
          <div className="space-y-1">
            <Label htmlFor="postazione-zona">Zona</Label>
            <NativeSelect id="postazione-zona" {...register('zonaId', { valueAsNumber: true })}>
              <option value="">Scegli una zona</option>
              {zone.data?.map((z) => (
                <option key={z.id} value={z.id}>
                  {z.nome}
                </option>
              ))}
            </NativeSelect>
            {errors.zonaId && (
              <p className="text-nota text-destructive">{errors.zonaId.message}</p>
            )}
          </div>
          <div className="flex items-center gap-2">
            <input
              id="postazione-attiva"
              {...register('attiva')}
              type="checkbox"
              className="h-4 w-4 accent-primary"
            />
            <Label htmlFor="postazione-attiva">Attiva</Label>
          </div>
          {/* Stesso piede di tutti gli altri modali: una riga di separazione, la rinuncia in
              chiaro e l'azione in pieno. Prima qui il pulsante di conferma era da solo e a
              tutta larghezza, senza modo di uscire se non con la X in alto. */}
          <div className="flex justify-end gap-2 border-t pt-4">
            <Button type="button" variant="ghost" onClick={onClose}>
              Annulla
            </Button>
            <Button type="submit" disabled={inCorso}>
              {inCorso ? 'Salvataggio…' : postazione ? 'Salva modifiche' : 'Crea tavolo'}
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  )
}
