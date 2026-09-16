import { useCallback, useEffect, useState } from 'react'
import { CHIAVE_TEMA, ThemeContext, type Tema } from '@/context/theme-context'

/**
 * Fase 13 — tema chiaro/scuro.
 *
 * Il blocco `.dark` esisteva in `index.css` fin dall'inizio, ma **nessuno aggiungeva quella
 * classe** al documento: era codice morto. Qui si chiude il giro.
 *
 * Da sapere: perché il tema scuro funzioni davvero non basta questo file. Serve che nessuna
 * pagina scriva colori fissi tipo `bg-white`, che resterebbero bianchi anche di notte. È il
 * lavoro fatto nel blocco 1.3 della stessa fase.
 */

/** Legge la preferenza salvata. In navigazione privata l'accesso può fallire: non deve rompere
 *  la pagina, si riparte semplicemente da "sistema". */
function leggiPreferenza(): Tema {
  try {
    const salvato = localStorage.getItem(CHIAVE_TEMA)
    if (salvato === 'chiaro' || salvato === 'scuro' || salvato === 'sistema') return salvato
  } catch {
    // niente da fare: si usa il valore predefinito
  }
  return 'sistema'
}

function sistemaPreferisceScuro(): boolean {
  return window.matchMedia?.('(prefers-color-scheme: dark)').matches ?? false
}

function applicaAlDocumento(effettivo: 'chiaro' | 'scuro') {
  document.documentElement.classList.toggle('dark', effettivo === 'scuro')
  // Fa disegnare al browser i suoi elementi (barre di scorrimento, menu nativi, campi data) con
  // la tinta giusta. Senza, in tema scuro compaiono barre bianche ai bordi.
  document.documentElement.style.colorScheme = effettivo === 'scuro' ? 'dark' : 'light'
}

export function ThemeProvider({ children }: { children: React.ReactNode }) {
  const [tema, setTema] = useState<Tema>(leggiPreferenza)
  const [sistemaScuro, setSistemaScuro] = useState(sistemaPreferisceScuro)

  const temaEffettivo: 'chiaro' | 'scuro' =
    tema === 'sistema' ? (sistemaScuro ? 'scuro' : 'chiaro') : tema

  // Chi sceglie "sistema" deve vedere l'app cambiare quando cambia il dispositivo, senza
  // ricaricare la pagina.
  useEffect(() => {
    const media = window.matchMedia('(prefers-color-scheme: dark)')
    const aggiorna = (e: MediaQueryListEvent) => setSistemaScuro(e.matches)
    media.addEventListener('change', aggiorna)
    return () => media.removeEventListener('change', aggiorna)
  }, [])

  useEffect(() => {
    applicaAlDocumento(temaEffettivo)
  }, [temaEffettivo])

  const impostaTema = useCallback((nuovo: Tema) => {
    setTema(nuovo)
    try {
      localStorage.setItem(CHIAVE_TEMA, nuovo)
    } catch {
      // la scelta vale comunque per questa sessione
    }
  }, [])

  return (
    <ThemeContext.Provider value={{ tema, temaEffettivo, impostaTema }}>
      {children}
    </ThemeContext.Provider>
  )
}
