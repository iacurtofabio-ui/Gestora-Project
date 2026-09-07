/**
 * REV-082: i path degli endpoint erano scritti inline in ogni hook, senza un punto unico da
 * cui vedere l'intera superficie dell'API o da cui correggere un percorso in caso di rename
 * lato backend. Raggruppati qui per area, con lo stesso raggruppamento dei controller
 * (vedi GestoraWebApi/CLAUDE.md, sezione "Endpoint - riferimento reale").
 */
export const Endpoints = {
  auth: {
    login: '/AuthenticationUser/login',
    register: '/AuthenticationUser/register',
    getUsers: '/AuthenticationUser/get-users',
    updateUser: (id: string) => `/AuthenticationUser/update-user/${id}`,
    deleteUser: (id: string) => `/AuthenticationUser/delete-user/${id}`,
    assignRole: '/AuthenticationUser/assign-role',
    removeRole: '/AuthenticationUser/remove-role',
    resetPassword: (id: string) => `/AuthenticationUser/reset-password/${id}`,
  },
  setup: {
    stato: '/Setup/stato',
    admin: '/Setup/admin',
  },
  dashboard: {
    giornaliera: (data: string) => `/Dashboard/giornaliera?data=${data}`,
    settimanale: (dataInizio: string) => `/Dashboard/settimanale?dataInizio=${dataInizio}`,
  },
  zona: {
    getAll: '/Zona/get-all-zone',
    getAttive: '/Zona/get-zone-attive',
    crea: '/Zona/crea-zona',
    update: '/Zona/update-zona',
    delete: (id: number) => `/Zona/delete-zona/${id}`,
  },
  fasciaOraria: {
    attive: '/FasceOrarie/fasce-attive',
    getAll: '/FasceOrarie/get-all-fasce',
    perGiorno: (giorno: number) => `/FasceOrarie/fasce-per-giorno?giorno=${giorno}`,
    crea: '/FasceOrarie/crea-fascia',
    update: '/FasceOrarie/update-fascia',
    delete: (id: number) => `/FasceOrarie/delete-fascia?id=${id}`,
  },
  postazione: {
    perZona: (zonaId: number) => `/Postazione/get-postazioni-per-zona?zonaId=${zonaId}`,
    riepilogoSala: '/Postazione/riepilogo-sala',
    crea: '/Postazione/crea-postazione',
    update: '/Postazione/update-postazione',
    delete: (id: number) => `/Postazione/delete-postazione?id=${id}`,
  },
  prenotazione: {
    getAll: '/Prenotazione/get-all-prenotazioni',
    mie: '/Prenotazione/get-mie-prenotazioni',
    // NEW-002: pubblico, nessuna autenticazione richiesta.
    checkDisponibilita: '/Prenotazione/check-disponibilita',
    crea: '/Prenotazione/crea-prenotazione',
    update: (id: number) => `/Prenotazione/update-prenotazione?id=${id}`,
    conferma: (id: number) => `/Prenotazione/conferma-prenotazione?id=${id}`,
    completa: (id: number) => `/Prenotazione/completa-prenotazione?id=${id}`,
    annulla: (id: number) => `/Prenotazione/annulla-prenotazione?id=${id}`,
    delete: (id: number) => `/Prenotazione/delete-prenotazione?id=${id}`,
  },
} as const
