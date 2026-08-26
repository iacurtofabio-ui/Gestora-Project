import { useDashboardGiornaliera, useDashboardSettimanale } from '@/hooks/useDashboard'

function getLunediSettimanaCorrente(): string {
  const oggi = new Date()
  const giorno = oggi.getDay()
  const lunedi = new Date(oggi)
  lunedi.setDate(oggi.getDate() - (giorno === 0 ? 6 : giorno - 1))
  return lunedi.toISOString().split('T')[0]
}

export default function DashboardPage() {
  const oggi = new Date().toISOString().split('T')[0]
  const giornaliera = useDashboardGiornaliera(oggi)
  const settimanale = useDashboardSettimanale(getLunediSettimanaCorrente())

  if (giornaliera.isLoading || settimanale.isLoading) return <div>Caricamento...</div>
  if (giornaliera.isError || settimanale.isError) return <div>Errore nel caricamento</div>

  return (
    <div className="p-6">
      <h1 className="text-2xl font-bold mb-6">Dashboard</h1>

      <h2 className="text-lg font-semibold text-gray-700 mb-3">Oggi</h2>

      {/* KPI giornalieri — 4 card separate */}
      <div className="grid grid-cols-4 gap-4 mb-6">
        <div className="bg-white rounded-lg p-4 border">
          <p className="text-sm text-gray-500">Totale Prenotazioni</p>
          <p className="text-2xl font-bold">{giornaliera.data?.totalePrenotazioni}</p>
        </div>
        <div className="bg-white rounded-lg p-4 border">
          <p className="text-sm text-gray-500">Prenotazioni Attive</p>
          <p className="text-2xl font-bold">{giornaliera.data?.prenotazioniAttive}</p>
        </div>
        <div className="bg-white rounded-lg p-4 border">
          <p className="text-sm text-gray-500">Postazioni Libere</p>
          <p className="text-2xl font-bold">{giornaliera.data?.postazioniLibere}</p>
        </div>
        <div className="bg-white rounded-lg p-4 border">
          <p className="text-sm text-gray-500">Postazioni Occupate</p>
          <p className="text-2xl font-bold">{giornaliera.data?.postazioniOccupate}</p>
        </div>
      </div>

      {/* Coperti per fascia oraria */}
      <div className="bg-white rounded-lg border mb-6">
        <h2 className="text-sm font-semibold text-gray-700 p-4 border-b">Coperti per Fascia Oraria</h2>
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b">
              <th className="text-left p-3">Fascia</th>
              <th className="text-left p-3">Coperti Prenotati</th>
              <th className="text-left p-3">Disponibili</th>
            </tr>
          </thead>
          <tbody>
            {giornaliera.data?.copertiPerFascia.length === 0 ? (
              <tr>
                <td colSpan={3} className="p-3 text-center text-gray-400">
                  Nessuna fascia oraria configurata per oggi
                </td>
              </tr>
            ) : (
              giornaliera.data?.copertiPerFascia.map((fascia) => (
                <tr key={fascia.fasciaOrariaId} className="border-b">
                  <td className="p-3">{fascia.oraInizio} - {fascia.oraFine}</td>
                  <td className="p-3">{fascia.copertiPrenotati}</td>
                  <td className="p-3">{fascia.copertiDisponibili}</td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      <h2 className="text-lg font-semibold text-gray-700 mb-3 mt-8">Questa settimana</h2>

      {/* KPI settimanali */}
      <div className="grid grid-cols-2 gap-4 mb-6">
        <div className="bg-white rounded-lg p-4 border">
          <p className="text-sm text-gray-500">Prenotazioni Settimana</p>
          <p className="text-2xl font-bold">{settimanale.data?.totalePrenotazioni}</p>
        </div>
        <div className="bg-white rounded-lg p-4 border">
          <p className="text-sm text-gray-500">Coperti Settimana</p>
          <p className="text-2xl font-bold">{settimanale.data?.totaleCoperti}</p>
        </div>
      </div>

      {/* Dettaglio giorni settimana */}
      <div className="bg-white rounded-lg border">
        <h2 className="text-sm font-semibold text-gray-700 p-4 border-b">Dettaglio Settimana</h2>
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b">
              <th className="text-left p-3">Giorno</th>
              <th className="text-left p-3">Prenotazioni</th>
              <th className="text-left p-3">Coperti</th>
              <th className="text-left p-3">Annullate</th>
            </tr>
          </thead>
          <tbody>
            {settimanale.data?.giorni.map((giorno) => (
              <tr key={giorno.data} className="border-b">
                <td className="p-3">{giorno.giornoNome}</td>
                <td className="p-3">{giorno.numeroPrenotazioni}</td>
                <td className="p-3">{giorno.numeroCoperti}</td>
                <td className="p-3">{giorno.annullate}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}
