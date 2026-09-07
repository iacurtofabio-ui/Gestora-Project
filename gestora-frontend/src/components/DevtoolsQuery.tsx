import { lazy, Suspense, type ComponentType } from 'react'

/**
 * REV-076 — i devtools di React Query solo in sviluppo.
 *
 * Prima erano importati e montati incondizionatamente in main.tsx: finivano nel pacchetto di
 * produzione e mostravano a chiunque il pannello con le chiavi delle query e i dati in cache.
 *
 * `import.meta.env.DEV` viene sostituito da Vite a build time con `false`, quindi in produzione il
 * ternario si risolve staticamente sul componente vuoto e l'import dinamico non viene nemmeno
 * generato: nessun chunk, nessun riferimento al pacchetto. E' la stessa proprieta' che in Fase 6 ha
 * permesso di verificare VITE_API_URL su Vercel guardando cosa *non* c'era nel bundle - e allo
 * stesso modo qui la verifica si fa cercando "ReactQueryDevtools" in dist/: non deve comparire.
 *
 * La condizione sta fuori dal componente di proposito: dentro, il ramo morto verrebbe eliminato ma
 * la chiamata a lazy() resterebbe, e con lei un chunk vuoto da scaricare.
 */
const Pannello: ComponentType<{ initialIsOpen?: boolean }> = import.meta.env.DEV
  ? lazy(() => import('@tanstack/react-query-devtools').then(m => ({ default: m.ReactQueryDevtools })))
  : () => null

export function DevtoolsQuery() {
  if (!import.meta.env.DEV) return null

  return (
    <Suspense fallback={null}>
      <Pannello initialIsOpen={false} />
    </Suspense>
  )
}
