import { Loader2Icon } from 'lucide-react'
import { AzioniRiga, type VoceAzione } from '@/components/AzioniRiga'
import { STATI_PRENOTAZIONE, type PrenotazioneDTO } from '@/types/prenotazione'

/**
 * Le azioni di una riga di prenotazione.
 *
 * La disposizione (una azione in chiaro, il resto nel menu «…», la distruttiva sotto un
 * separatore) sta in [AzioniRiga], condivisa con le altre pagine a elenco. Qui resta solo la
 * parte di dominio: quale azione ha senso in quale stato, e chi puo' farla.
 *
 * Nessuna regola di permessi e' cambiata rispetto ai cinque pulsanti affiancati di prima: sono le
 * stesse condizioni (isStaff, isAdmin, stati ammessi dal backend), solo disposte diversamente.
 */

type Props = {
  prenotazione: PrenotazioneDTO
  isStaff: boolean
  isAdmin: boolean
  /** L'azione primaria di questa riga e' in corso: si disabilita e mostra la rotella. */
  inCorso: boolean
  onConferma: () => void
  onCompleta: () => void
  onModifica: () => void
  onAnnulla: () => void
  onElimina: () => void
}

export function AzioniPrenotazione({
  prenotazione: p,
  isStaff,
  isAdmin,
  inCorso,
  onConferma,
  onCompleta,
  onModifica,
  onAnnulla,
  onElimina,
}: Props) {
  const attiva = p.stato === STATI_PRENOTAZIONE.ATTIVA
  const confermata = p.stato === STATI_PRENOTAZIONE.IN_CORSO
  const annullata = p.stato === STATI_PRENOTAZIONE.ANNULLATA

  // L'azione in chiaro e' quella che lo stato rende ovvia. Su Completata e Annullata non ce n'e'
  // nessuna: non c'e' piu' niente da fare.
  const azionePrimaria =
    isStaff && attiva
      ? { etichetta: 'Conferma', onSelect: onConferma }
      : isStaff && confermata
        ? { etichetta: 'Completa', onSelect: onCompleta }
        : undefined

  const voci: VoceAzione[] = []
  // NEW-001: la modifica si offre solo sulle prenotazioni ancora Attive; per il Cliente il
  // preavviso minimo di 2h lo verifica il backend e l'eventuale rifiuto arriva come toast.
  if (attiva) voci.push({ etichetta: 'Modifica prenotazione', onSelect: onModifica })
  // RBAC-002: il Cliente puo' annullare una propria prenotazione (la lista che vede e' gia'
  // filtrata sulle sue), entro il cutoff verificato dal backend.
  if (attiva || confermata) voci.push({ etichetta: 'Annulla prenotazione', onSelect: onAnnulla })

  // NEW-004: il backend accetta l'eliminazione solo su Attiva o Annullata. Fuori da quegli stati
  // la voce non si mostra, invece di far scoprire il limite con un 409.
  const distruttiva =
    isAdmin && (attiva || annullata)
      ? { etichetta: 'Elimina definitivamente', onSelect: onElimina }
      : undefined

  return (
    <AzioniRiga
      descrizione={`la prenotazione di ${p.nomeCliente ?? p.nomeUtente ?? 'cliente'}`}
      azionePrimaria={
        azionePrimaria && {
          ...azionePrimaria,
          inCorso,
          icona: inCorso ? <Loader2Icon className="animate-spin" /> : undefined,
        }
      }
      voci={voci}
      distruttiva={distruttiva}
    />
  )
}
