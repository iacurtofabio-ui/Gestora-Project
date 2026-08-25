using GestoraWebApi.Auth;
using GestoraWebApi.Enums;
using GestoraWebApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GestoraWebApi.Context
{
    public class GestoraContext : IdentityDbContext<ApplicationUser>
    {

        public DbSet<Logging> LogActivities { get; set; }
        public DbSet<Postazione> Postazioni { get; set; }
        public DbSet<FasciaOraria> FasciaOrarie { get; set; }
        public DbSet<Prenotazione> Prenotazioni { get; set; }
        public DbSet<PrenotazionePostazione> PrenotazioniPostazioni { get; set; }
        public DbSet<Zona> Zone { get; set; }



        public GestoraContext(DbContextOptions<GestoraContext> options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ApplicationUser>().ToTable("Utenti");
            modelBuilder.Entity<IdentityRole>().ToTable("Ruoli");

            //POSTAZIONI
            modelBuilder.Entity<Postazione>(entity =>
            {
                entity.ToTable("Postazioni");

                entity.HasKey(p => p.Id).HasName("Postazione_Pkey");

                entity.Property(p => p.Numero)
                    .IsRequired()
                    .HasColumnName("Numero");

                entity.Property(p => p.CapienzaMassima)
                    .IsRequired()
                    .HasColumnName("CapienzaMassima");

                //entity.Property(p => p.Zona)
                //    .IsRequired()
                //    .HasColumnName("Zona");

                entity.Property(p => p.Attiva)
                    .IsRequired()
                    .HasColumnName("Attiva");                
            });

            //FASCE ORARIE
            modelBuilder.Entity<FasciaOraria>(entity =>
            {
                entity.ToTable("FasceOrarie");

                entity.HasKey(f => f.Id).HasName("FasciaOraria_Pkey");

                entity.Property(f => f.OrarioInizio)
                    .IsRequired()
                    .HasColumnName("OrarioInizio");

                entity.Property(f => f.OrarioFine)
                    .IsRequired()
                    .HasColumnName("OrarioFine");

                // mappo l'enum DayOfWeek come int su DB
                entity.Property(f => f.GiornoSettimana)
                    .HasConversion<int>()
                    .IsRequired()
                    .HasColumnName("GiornoSettimana");

                entity.Property(f => f.MaxPrenotazioni)
                    .IsRequired()
                    .HasColumnName("MaxPrenotazioni");

                entity.Property(f => f.Attiva)
                    .IsRequired()
                    .HasColumnName("Attiva");
            });

            //PRENOTAZIONI

            modelBuilder.Entity<Prenotazione>(entity =>
            {
                entity.ToTable("Prenotazioni");

                entity.HasKey(p => p.Id).HasName("Prenotazione_Pkey");

                entity.Property(p => p.DataPrenotazione)
                    .IsRequired()
                    .HasColumnName("DataPrenotazione");

                entity.Property(p => p.NumeroCoperti)
                    .IsRequired()
                    .HasColumnName("NumeroCoperti");

                entity.Property(p => p.Stato)
                    .IsRequired()
                    .HasColumnName("Stato")
                    .HasConversion<string>();

                entity.Property(p => p.Note)
                    .HasColumnName("Note");

                entity.HasOne(p => p.User)
                    .WithMany(u => u.Prenotazioni)
                    .HasForeignKey(p => p.UserId)
                    .OnDelete(DeleteBehavior.Cascade);//Tutti le prenotazioni associate a quell'utente vengono automaticamente eliminati


                entity.HasOne(p => p.FasciaOraria)
                    .WithMany(f => f.Prenotazioni)
                    .HasForeignKey(p => p.FasciaOrariaId);
                    

                entity.HasIndex(p => new { p.UserId, p.DataPrenotazione })
                      .IsUnique()//nessun record può avere la stessa combinazione di UserId e DataPrenotazione, escluse le annullate
                      .HasFilter("\"Stato\" <> 'Annullata'")
                      .HasDatabaseName("UX_Prenotazione_User_DataPrenotazione");//nome dell'indice

            });

            //PRENOTAZIONE POSTAZIONE
            modelBuilder.Entity<PrenotazionePostazione>(entity =>
            {
                entity.ToTable("PrenotazioniPostazioni");

                entity.HasKey(pp => new { pp.PostazioneId, pp.PrenotazioneId })
                      .HasName("PrenotazionePostazione_Pkey");

                entity.HasOne(pp => pp.Postazione)
                    .WithMany(p => p.PrenotazioniPostazioni)
                    .HasForeignKey(pp => pp.PostazioneId);

                entity.HasOne(pp => pp.Prenotazione)
                    .WithMany(pr => pr.PrenotazioniPostazioni)
                    .HasForeignKey(pp => pp.PrenotazioneId);

                entity.Property(pp => pp.NumeroPosti)                    
                    .HasColumnName("NumeroPosti");

            });

            //ZONA
            modelBuilder.Entity<Zona>(entity =>
            {
                entity.ToTable("Zone");

                entity.HasKey(z => z.Id).HasName("Zona_Pkey");

                entity.Property(z => z.Nome)
                    .IsRequired()
                    .HasColumnName("Nome");
                    

                entity.Property(z => z.Attiva)
                    .IsRequired()
                    .HasColumnName("Attiva");

                entity.HasMany(z => z.Postazioni)
                    .WithOne(p => p.Zona)
                    .HasForeignKey(p => p.ZonaId);
            });
        }

    }
}
