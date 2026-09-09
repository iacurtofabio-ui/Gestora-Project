using GestoraWebApi.Auth;
using GestoraWebApi.Context;
using GestoraWebApi.Enums;
using GestoraWebApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GestoraWebApi.Development
{
    /// <summary>
    /// Popolamento del database LOCALE con un dataset pensato per rompere l'interfaccia.
    ///
    /// PERCHE' ESISTE
    /// Provare un redesign su dati ordinati non prova niente: i nomi corti stanno in colonna, le
    /// liste corte non vanno mai a capo, e i casi limite non si vedono finche' non capitano a un
    /// cliente vero. Questo seed mette dentro apposta quello che di solito manca — nomi lunghi,
    /// liste dense, una fascia oltre il tetto, una zona vuota.
    ///
    /// NON E' UN SEED DI PRODUZIONE. Cancella i dati di dominio prima di riscriverli.
    ///
    /// TRE PROTEZIONI, perche' un comando che cancella dati non deve poter partire per sbaglio:
    ///   1. gira solo con ASPNETCORE_ENVIRONMENT=Development;
    ///   2. gira solo se il comando ha l'argomento esplicito (--seed-sviluppo);
    ///   3. si rifiuta di partire se la stringa di connessione non punta a questa macchina.
    /// La terza e' quella che conta: le prime due si aggirano per distrazione, un host remoto no.
    ///
    /// Uso (dalla cartella GestoraWebApi):
    ///   dotnet run -- --seed-sviluppo
    ///   dotnet run -- --reimposta-password mario@esempio.it NuovaPassword1!
    /// </summary>
    public static class SeedSviluppo
    {
        public const string ArgomentoSeed = "--seed-sviluppo";
        public const string ArgomentoPassword = "--reimposta-password";

        /// <summary>
        /// Variante del seed che riempie OGNI fascia di oggi oltre il proprio tetto.
        ///
        /// Serve a un caso solo: la Dashboard mostra "N oltre il tetto" quando i coperti del
        /// giorno superano la capienza complessiva. Con dati realistici quello stato non si
        /// raggiunge mai — bastano poche fasce vuote a riportare il totale in positivo — quindi
        /// senza questa variante quel ramo dell'interfaccia non e' provabile a mano.
        ///
        ///   dotnet run -- --seed-sviluppo --giornata-piena
        /// </summary>
        public const string ArgomentoGiornataPiena = "--giornata-piena";

        /// <summary>Credenziali dell'amministratore di sviluppo, ricreate a ogni seed.</summary>
        public const string AdminEmail = "admin@gestora.local";
        public const string AdminPassword = "Sviluppo1!";

        /// <summary>
        /// Rifiuta qualsiasi database che non sia su questa macchina. Un seed che cancella e
        /// riscrive non deve avere la possibilita' tecnica di toccare Railway.
        /// </summary>
        private static void VerificaCheSiaLocale(string? connectionString)
        {
            var host = new NpgsqlConnectionStringBuilder(connectionString).Host?.ToLowerInvariant();

            var locale = host is "localhost" or "127.0.0.1" or "::1" or "host.docker.internal";
            if (!locale)
            {
                throw new InvalidOperationException(
                    $"Il seed di sviluppo si rifiuta di partire: il database non e' locale (host: '{host}'). " +
                    "Questo comando cancella i dati di dominio e puo' essere usato solo sulla macchina di sviluppo.");
            }
        }

        // ---------------------------------------------------------------------------------
        // Reimpostazione password
        // ---------------------------------------------------------------------------------

        /// <summary>
        /// Rimette una password nota a un utente esistente, passando da Identity.
        ///
        /// Non si fa con una UPDATE in SQL: l'hash di Identity ha un formato proprio (versione,
        /// salt, iterazioni) e scriverlo a mano produce un utente che esiste e non entra mai.
        /// </summary>
        public static async Task ReimpostaPasswordAsync(
            IServiceProvider servizi,
            string email,
            string nuovaPassword,
            ILogger logger)
        {
            using var scope = servizi.CreateScope();
            var configurazione = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            VerificaCheSiaLocale(configurazione.GetConnectionString("DefaultConnection"));

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var utente = await userManager.FindByEmailAsync(email);
            if (utente is null)
            {
                logger.LogError("Nessun utente con email {Email}.", email);
                return;
            }

            var token = await userManager.GeneratePasswordResetTokenAsync(utente);
            var esito = await userManager.ResetPasswordAsync(utente, token, nuovaPassword);

            if (!esito.Succeeded)
            {
                logger.LogError(
                    "Password non reimpostata: {Errori}",
                    string.Join(" | ", esito.Errors.Select(e => e.Description)));
                return;
            }

            logger.LogInformation("Password di {Email} reimpostata.", email);
        }

        // ---------------------------------------------------------------------------------
        // Seed
        // ---------------------------------------------------------------------------------

        public static async Task EseguiAsync(
            IServiceProvider servizi, ILogger logger, bool giornataPiena = false)
        {
            using var scope = servizi.CreateScope();
            var configurazione = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            VerificaCheSiaLocale(configurazione.GetConnectionString("DefaultConnection"));

            var db = scope.ServiceProvider.GetRequiredService<GestoraContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            logger.LogInformation("Seed di sviluppo: cancello i dati di dominio esistenti.");

            // L'ordine segue le dipendenze: prima le associazioni, poi cio' che vi punta.
            db.PrenotazioniPostazioni.RemoveRange(db.PrenotazioniPostazioni);
            db.Prenotazioni.RemoveRange(db.Prenotazioni);
            db.Postazioni.RemoveRange(db.Postazioni);
            db.Zone.RemoveRange(db.Zone);
            db.FasciaOrarie.RemoveRange(db.FasciaOrarie);
            await db.SaveChangesAsync();

            var utenti = await CreaUtentiAsync(userManager, logger);
            var zone = await CreaZoneAsync(db);
            await CreaTavoliAsync(db, zone);
            var fasce = await CreaFasceAsync(db);
            await CreaPrenotazioniAsync(db, fasce, utenti, logger, giornataPiena);

            logger.LogInformation(
                "Seed completato. Accesso: {Email} / {Password}", AdminEmail, AdminPassword);
        }

        // ---------------------------------------------------------------------------------
        // Utenti
        // ---------------------------------------------------------------------------------

        private static async Task<List<ApplicationUser>> CreaUtentiAsync(
            UserManager<ApplicationUser> userManager, ILogger logger)
        {
            // CASO LIMITE: un utente con TUTTI E TRE i ruoli. E' legittimo (vedi CLAUDE.md, RBAC)
            // e nella colonna Ruoli produce la stringa piu' lunga possibile.
            // CASO LIMITE: un nome e un'email lunghi, per vedere se la colonna tronca o sfonda.
            var daCreare = new[]
            {
                (Nome: "admin", Email: AdminEmail, Ruoli: new[] { Roles.Admin }),
                (Nome: "Maria Vittoria Della Rovere Castiglioni", Email: "mariavittoria.dellarovere.castiglioni@ristorantesanmartino.example.com", Ruoli: new[] { Roles.Admin, Roles.Staff, Roles.Cliente }),
                (Nome: "staff", Email: "staff@gestora.local", Ruoli: new[] { Roles.Staff }),
                (Nome: "cliente", Email: "cliente@gestora.local", Ruoli: new[] { Roles.Cliente }),
                // CASO LIMITE: un utente senza nessun ruolo. Puo' succedere togliendo l'ultimo
                // ruolo dalla schermata Ruoli, e la cella non deve restare vuota e muta.
                (Nome: "senza.ruoli", Email: "senzaruoli@gestora.local", Ruoli: Array.Empty<string>()),
            };

            var creati = new List<ApplicationUser>();

            foreach (var (nome, email, ruoli) in daCreare)
            {
                var utente = await userManager.FindByEmailAsync(email);

                if (utente is null)
                {
                    utente = new ApplicationUser
                    {
                        UserName = nome,
                        Email = email,
                        EmailConfirmed = true
                    };
                    var esito = await userManager.CreateAsync(utente, AdminPassword);
                    if (!esito.Succeeded)
                    {
                        logger.LogError(
                            "Utente {Email} non creato: {Errori}",
                            email, string.Join(" | ", esito.Errors.Select(e => e.Description)));
                        continue;
                    }
                }
                else
                {
                    // Utente gia' presente da un seed precedente: si riallinea la password, cosi'
                    // le credenziali scritte nella documentazione restano vere.
                    var token = await userManager.GeneratePasswordResetTokenAsync(utente);
                    await userManager.ResetPasswordAsync(utente, token, AdminPassword);
                }

                var ruoliAttuali = await userManager.GetRolesAsync(utente);
                await userManager.RemoveFromRolesAsync(utente, ruoliAttuali);
                if (ruoli.Length > 0)
                    await userManager.AddToRolesAsync(utente, ruoli);

                creati.Add(utente);
            }

            return creati;
        }

        // ---------------------------------------------------------------------------------
        // Zone e tavoli
        // ---------------------------------------------------------------------------------

        private static async Task<List<Zona>> CreaZoneAsync(GestoraContext db)
        {
            var zone = new List<Zona>
            {
                new() { Nome = "Sala interna", Attiva = true },
                new() { Nome = "Dehors", Attiva = true },
                // CASO LIMITE: nome lungo senza spazi utili dove andare a capo.
                new() { Nome = "Sala privata al primo piano con vista sul giardino d'inverno", Attiva = true },
                // CASO LIMITE: zona disattivata.
                new() { Nome = "Veranda (chiusa per lavori)", Attiva = false },
                // CASO LIMITE: zona SENZA TAVOLI. La pagina Tavoli deve dire che e' vuota e
                // proporre di aggiungerne uno, non sembrare rotta.
                new() { Nome = "Sala nuova", Attiva = true },
            };

            db.Zone.AddRange(zone);
            await db.SaveChangesAsync();
            return zone;
        }

        private static async Task CreaTavoliAsync(GestoraContext db, List<Zona> zone)
        {
            var tavoli = new List<Postazione>();
            var numero = 1;

            // "Sala nuova" (l'ultima) resta apposta senza tavoli.
            foreach (var zona in zone.Take(zone.Count - 1))
            {
                var quanti = zona.Nome == "Sala interna" ? 14 : 6;
                for (var i = 0; i < quanti; i++)
                {
                    tavoli.Add(new Postazione
                    {
                        Numero = numero++,
                        // Capienze miste: la decisione 5 ammette qualsiasi numero da 1 in su, e
                        // la decisione 3 da' il bonus di testata solo alle unioni di soli tavoli
                        // da 2. Servono quindi entrambi i casi per provare l'assegnazione.
                        CapienzaMassima = (i % 4) switch { 0 => 2, 1 => 4, 2 => 2, _ => 6 },
                        Attiva = i != 5,   // CASO LIMITE: un tavolo disattivato per zona
                        ZonaId = zona.Id
                    });
                }
            }

            // CASO LIMITE: un tavolo da 1 posto e uno molto grande, agli estremi della scala.
            tavoli.Add(new Postazione { Numero = numero++, CapienzaMassima = 1, Attiva = true, ZonaId = zone[0].Id });
            tavoli.Add(new Postazione { Numero = numero++, CapienzaMassima = 12, Attiva = true, ZonaId = zone[2].Id });

            db.Postazioni.AddRange(tavoli);
            await db.SaveChangesAsync();
        }

        // ---------------------------------------------------------------------------------
        // Fasce orarie
        // ---------------------------------------------------------------------------------

        private static async Task<List<FasciaOraria>> CreaFasceAsync(GestoraContext db)
        {
            var fasce = new List<FasciaOraria>();
            var oggi = DateOnly.FromDateTime(DateTime.UtcNow).DayOfWeek;

            // CASO LIMITE: OGGI ha 14 fasce, una ogni ora dalle 8 alle 22.
            // Non e' realistico per un ristorante ed e' voluto: la Dashboard elenca le fasce del
            // giorno una per riga, e con quattro fasce sta comoda. Con quattordici si vede se
            // l'elenco regge, se la pagina diventa uno scorrimento infinito e se la banda grande
            // resta il primo elemento che si guarda.
            // Sono contigue e non sovrapposte, altrimenti il controllo della Fase 11 le
            // rifiuterebbe quando le si modifica dall'interfaccia.
            for (var ora = 8; ora < 22; ora++)
            {
                fasce.Add(new FasciaOraria
                {
                    OrarioInizio = new TimeOnly(ora, 0),
                    OrarioFine = new TimeOnly(ora, 59),
                    GiornoSettimana = oggi,
                    // Tetti diversi per avere bande di lunghezze diverse invece di una scala
                    // regolare, che nasconderebbe gli errori di allineamento.
                    MaxCoperti = 20 + (ora % 5) * 8,
                    Attiva = true
                });
            }

            // Gli altri giorni: due turni ciascuno, cosi' la pagina Fasce supera comunque le 20
            // righe complessive (14 + 12 = 26) e si puo' guardare come si comporta la tabella.
            foreach (DayOfWeek giorno in Enum.GetValues<DayOfWeek>())
            {
                if (giorno == oggi) continue;

                fasce.Add(new FasciaOraria
                {
                    OrarioInizio = new TimeOnly(12, 0),
                    OrarioFine = new TimeOnly(14, 30),
                    GiornoSettimana = giorno,
                    MaxCoperti = 40,
                    Attiva = true
                });
                fasce.Add(new FasciaOraria
                {
                    OrarioInizio = new TimeOnly(19, 0),
                    OrarioFine = new TimeOnly(23, 0),
                    GiornoSettimana = giorno,
                    MaxCoperti = 56,
                    // CASO LIMITE: una fascia disattivata, per vedere lo stato "Non attiva".
                    Attiva = giorno != DayOfWeek.Monday
                });
            }

            db.FasciaOrarie.AddRange(fasce);
            await db.SaveChangesAsync();
            return fasce;
        }

        // ---------------------------------------------------------------------------------
        // Prenotazioni
        // ---------------------------------------------------------------------------------

        private static readonly string[] NomiCliente =
        {
            "Rossi",
            "Bianchi",
            // CASO LIMITE: il nome piu' lungo che ci si possa aspettare davvero.
            "Maria Vittoria Della Rovere Castiglioni-Buonaparte",
            "Ng",                                   // e il piu' corto
            "Famiglia Esposito-Sanfilippo (tavolo lungo)",
            "Colombo",
            "O'Brien-Smith",                        // apostrofo e trattino
            "Müller-Ødegård",                       // caratteri non ASCII
        };

        // CASO LIMITE: una nota lunghissima. Nel form e' un textarea, ma il valore viaggia anche
        // nelle risposte dell'API e un domani potrebbe finire in tabella.
        private const string NotaLunga =
            "Allergia a crostacei e frutta a guscio per due commensali, uno dei quali intollerante " +
            "anche al lattosio. Serve un seggiolone e, se possibile, un tavolo lontano dalla porta " +
            "d'ingresso perche' c'e' una signora anziana. Arriveranno probabilmente con venti " +
            "minuti di ritardo rispetto all'orario prenotato: hanno chiesto di tenere il tavolo. " +
            "Festeggiano un anniversario, gradirebbero una candelina sul dolce a fine cena.";

        private static async Task CreaPrenotazioniAsync(
            GestoraContext db,
            List<FasciaOraria> fasce,
            List<ApplicationUser> utenti,
            ILogger logger,
            bool giornataPiena)
        {
            if (utenti.Count == 0)
            {
                logger.LogWarning("Nessun utente creato: salto le prenotazioni.");
                return;
            }

            var oggi = DateOnly.FromDateTime(DateTime.UtcNow);
            var fasceDiOggi = fasce.Where(f => f.GiornoSettimana == oggi.DayOfWeek)
                                   .OrderBy(f => f.OrarioInizio)
                                   .ToList();

            var prenotazioni = new List<Prenotazione>();
            var casuale = new Random(20260909);   // seme fisso: due esecuzioni danno lo stesso esito

            for (var i = 0; i < fasceDiOggi.Count; i++)
            {
                var fascia = fasceDiOggi[i];

                // Tre casi costruiti apposta, uno per ciascuno dei primi tre turni:
                //   0 -> ESATTAMENTE AL TETTO: la fascia mostra "0 disponibili" e la scritta
                //        "pieno". E' il caso che lo staff cerca a colpo d'occhio.
                //   1 -> OLTRE IL TETTO: piu' coperti della capienza. Il modello non lo impedisce
                //        a livello di database (il limite lo applica il servizio in fase di
                //        creazione), quindi si puo' ottenere solo da qui.
                //        ⚠️ Sul singolo turno resta invisibile: la Dashboard calcola i disponibili
                //        con Math.Max(0, ...), quindi si legge "0" come per una fascia piena.
                //        Si vede solo nel totale di giornata, che e' la somma vera.
                //   2 -> QUASI PIENO: sopra l'85%, la soglia che fa virare la banda su "attenzione".
                // Con --giornata-piena ogni fascia va sopra il proprio tetto: e' l'unico modo
                // per portare il totale della giornata oltre la capienza complessiva.
                var copertiDaRaggiungere = giornataPiena ? fascia.MaxCoperti + 5 : i switch
                {
                    0 => fascia.MaxCoperti,
                    1 => fascia.MaxCoperti + 9,
                    2 => (int)(fascia.MaxCoperti * 0.9),
                    _ => casuale.Next(0, fascia.MaxCoperti / 2)
                };

                var copertiMessi = 0;
                var indice = 0;

                while (copertiMessi < copertiDaRaggiungere)
                {
                    var coperti = Math.Min(casuale.Next(2, 7), copertiDaRaggiungere - copertiMessi);
                    if (coperti <= 0) break;

                    var utente = utenti[casuale.Next(utenti.Count)];

                    prenotazioni.Add(new Prenotazione
                    {
                        DataPrenotazione = oggi,
                        NumeroCoperti = coperti,
                        FasciaOrariaId = fascia.Id,
                        UserId = utente.Id,
                        // Tutti e quattro gli stati sono rappresentati: la colonna Stato e le
                        // azioni di riga cambiano per ciascuno.
                        Stato = (indice % 5) switch
                        {
                            0 => StatoPrenotazione.Attiva,
                            1 => StatoPrenotazione.InCorso,
                            2 => StatoPrenotazione.Completata,
                            3 => StatoPrenotazione.Attiva,
                            _ => StatoPrenotazione.InCorso
                        },
                        NomeCliente = indice % 3 == 0 ? NomiCliente[indice % NomiCliente.Length] : null,
                        Note = indice % 6 == 0 ? NotaLunga : null
                    });

                    copertiMessi += coperti;
                    indice++;
                }
            }

            // CASO LIMITE: prenotazioni annullate. Non contano nei totali (il servizio le esclude)
            // ma devono comparire in elenco, con lo stato barrato e senza azione primaria.
            for (var i = 0; i < 4; i++)
            {
                prenotazioni.Add(new Prenotazione
                {
                    DataPrenotazione = oggi,
                    NumeroCoperti = 2 + i,
                    FasciaOrariaId = fasceDiOggi[i % fasceDiOggi.Count].Id,
                    UserId = utenti[i % utenti.Count].Id,
                    Stato = StatoPrenotazione.Annullata,
                    NomeCliente = NomiCliente[i % NomiCliente.Length]
                });
            }

            // CASO LIMITE: abbastanza righe da riempire piu' pagine (la pagina ne mostra 20),
            // distribuite sui giorni successivi cosi' il filtro per data ha qualcosa da filtrare
            // e la paginazione qualcosa da paginare.
            var fasceAltriGiorni = fasce.Where(f => f.GiornoSettimana != oggi.DayOfWeek && f.Attiva).ToList();
            for (var giorno = 1; giorno <= 20; giorno++)
            {
                var data = oggi.AddDays(giorno);
                var fasceDelGiorno = fasceAltriGiorni.Where(f => f.GiornoSettimana == data.DayOfWeek).ToList();
                if (fasceDelGiorno.Count == 0) continue;

                for (var k = 0; k < 4; k++)
                {
                    prenotazioni.Add(new Prenotazione
                    {
                        DataPrenotazione = data,
                        NumeroCoperti = casuale.Next(2, 9),
                        FasciaOrariaId = fasceDelGiorno[k % fasceDelGiorno.Count].Id,
                        UserId = utenti[casuale.Next(utenti.Count)].Id,
                        Stato = StatoPrenotazione.Attiva,
                        NomeCliente = k % 2 == 0 ? NomiCliente[(giorno + k) % NomiCliente.Length] : null
                    });
                }
            }

            db.Prenotazioni.AddRange(prenotazioni);
            await db.SaveChangesAsync();

            await AssegnaTavoliAsync(db, prenotazioni, logger);

            logger.LogInformation(
                "Create {Totale} prenotazioni, di cui {Oggi} per oggi su {Fasce} fasce.",
                prenotazioni.Count,
                prenotazioni.Count(p => p.DataPrenotazione == oggi),
                fasceDiOggi.Count);
        }

        /// <summary>
        /// Assegna i tavoli alle prenotazioni di oggi.
        ///
        /// Non riusa il servizio di assegnazione di proposito: quello rispetta il tetto e
        /// rifiuterebbe proprio i casi limite che questo seed deve produrre. Qui l'assegnazione e'
        /// grezza e serve solo a riempire la colonna Tavoli con qualcosa di plausibile.
        /// Una prenotazione su cinque resta senza tavoli: e' un caso reale (annullata, o non
        /// ancora assegnata) e la cella deve mostrare un trattino, non restare vuota.
        /// </summary>
        private static async Task AssegnaTavoliAsync(
            GestoraContext db, List<Prenotazione> prenotazioni, ILogger logger)
        {
            var tavoli = await db.Postazioni.Where(p => p.Attiva).ToListAsync();
            if (tavoli.Count == 0)
            {
                logger.LogWarning("Nessun tavolo attivo: salto l'assegnazione.");
                return;
            }

            var associazioni = new List<PrenotazionePostazione>();
            var occupatiPerFascia = new Dictionary<(DateOnly, long), HashSet<long>>();

            var indice = 0;
            foreach (var prenotazione in prenotazioni.Where(p => p.Stato != StatoPrenotazione.Annullata))
            {
                indice++;
                if (indice % 5 == 0) continue;   // una su cinque resta senza tavolo

                var chiave = (prenotazione.DataPrenotazione, prenotazione.FasciaOrariaId);
                if (!occupatiPerFascia.TryGetValue(chiave, out var occupati))
                {
                    occupati = new HashSet<long>();
                    occupatiPerFascia[chiave] = occupati;
                }

                // L'indice unico UX_PrenotazionePostazione_Slot impedisce di assegnare lo stesso
                // tavolo due volte nella stessa data e fascia: qui lo si rispetta a mano.
                var libero = tavoli.FirstOrDefault(t => !occupati.Contains(t.Id));
                if (libero is null) continue;

                occupati.Add(libero.Id);
                associazioni.Add(new PrenotazionePostazione
                {
                    PrenotazioneId = prenotazione.Id,
                    PostazioneId = libero.Id,
                    NumeroPosti = prenotazione.NumeroCoperti,
                    DataPrenotazione = prenotazione.DataPrenotazione,
                    FasciaOrariaId = prenotazione.FasciaOrariaId
                });

                // CASO LIMITE: un'unione di piu' tavoli, che nella colonna Tavoli produce una
                // cella con piu' numeri e mette alla prova il troncamento.
                if (prenotazione.NumeroCoperti >= 6)
                {
                    var secondo = tavoli.FirstOrDefault(t => !occupati.Contains(t.Id));
                    if (secondo is not null)
                    {
                        occupati.Add(secondo.Id);
                        associazioni.Add(new PrenotazionePostazione
                        {
                            PrenotazioneId = prenotazione.Id,
                            PostazioneId = secondo.Id,
                            NumeroPosti = 0,
                            DataPrenotazione = prenotazione.DataPrenotazione,
                            FasciaOrariaId = prenotazione.FasciaOrariaId
                        });
                    }
                }
            }

            db.PrenotazioniPostazioni.AddRange(associazioni);
            await db.SaveChangesAsync();
        }
    }
}
