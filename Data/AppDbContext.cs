using Microsoft.EntityFrameworkCore;
using NfcHomeManager.Models;

namespace NfcHomeManager.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Mistnost> Mistnosti => Set<Mistnost>();
    public DbSet<Kategorie> Kategorie => Set<Kategorie>();
    public DbSet<Polozka> Polozky => Set<Polozka>();
    public DbSet<ServisniZaznam> ServisniZaznamy => Set<ServisniZaznam>();
    public DbSet<Termin> Terminy => Set<Termin>();
    public DbSet<KontrolaSady> KontrolySady => Set<KontrolaSady>();
    public DbSet<ProvedenaOperace> ProvedeneOperace => Set<ProvedenaOperace>();
    public DbSet<Pojisteni> Pojisteni => Set<Pojisteni>();
    public DbSet<Lek> Leky => Set<Lek>();
    public DbSet<LekovyKatalog> LekovyKatalog => Set<LekovyKatalog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Polozka>(entity =>
        {
            entity.HasIndex(p => p.Kod).IsUnique();
            entity.HasIndex(p => p.NfcUid).IsUnique().HasFilter("NfcUid IS NOT NULL");
            entity.Property(p => p.CenaKc).HasColumnType("decimal(18,2)");
            entity.Property(p => p.Mnozstvi).HasColumnType("decimal(18,3)");
            entity.Property(p => p.Rezim).HasConversion<string>();
            entity.Property(p => p.Specializace).HasConversion<string>();

            entity.HasOne(p => p.Kategorie)
                .WithMany(k => k.Polozky)
                .HasForeignKey(p => p.KategorieId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(p => p.Mistnost)
                .WithMany(m => m.Polozky)
                .HasForeignKey(p => p.MistnostId)
                .OnDelete(DeleteBehavior.SetNull);

            // Kontejner je take Polozka (sebe-vazba) - kdyz se krabice smaze,
            // obsah se jen odpoji, nemaze se s ni.
            entity.HasOne(p => p.Kontejner)
                .WithMany(p => p.Obsah)
                .HasForeignKey(p => p.KontejnerId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Lek>(entity =>
        {
            entity.Property(l => l.Mnozstvi).HasColumnType("decimal(18,3)");
            entity.Property(l => l.CilovaZasoba).HasColumnType("decimal(18,3)");

            entity.HasOne(l => l.Lekarnicka)
                .WithMany(p => p.Leky)
                .HasForeignKey(l => l.LekarnickaId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ServisniZaznam>(entity =>
        {
            entity.Property(s => s.CenaKc).HasColumnType("decimal(18,2)");
            entity.Property(s => s.Typ).HasConversion<string>();
            entity.Property(s => s.DalsiTerminTyp).HasConversion<string>();

            entity.HasOne(s => s.Polozka)
                .WithMany(p => p.ServisniZaznamy)
                .HasForeignKey(s => s.PolozkaId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Termin>(entity =>
        {
            entity.Property(t => t.Typ).HasConversion<string>();
            entity.HasIndex(t => new { t.PolozkaId, t.Typ }).IsUnique();

            entity.HasOne(t => t.Polozka)
                .WithMany(p => p.Terminy)
                .HasForeignKey(t => t.PolozkaId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<KontrolaSady>(entity =>
        {
            entity.Ignore(k => k.Uplna);

            entity.HasOne(k => k.Polozka)
                .WithMany(p => p.Kontroly)
                .HasForeignKey(k => k.PolozkaId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProvedenaOperace>(entity =>
        {
            entity.Property(o => o.Id).ValueGeneratedNever();
            entity.HasIndex(o => o.VytvorenoUtc);
        });

        modelBuilder.Entity<LekovyKatalog>(entity =>
        {
            // Neni unique - stejny EAN se muze v exportu SUKL objevit vicekrat
            // (napr. reregistrace), berem prvni shodu.
            entity.HasIndex(l => l.Ean);
        });

        modelBuilder.Entity<Pojisteni>(entity =>
        {
            entity.Property(i => i.RocniCenaKc).HasColumnType("decimal(18,2)");

            entity.HasOne(i => i.Polozka)
                .WithMany(p => p.Pojisteni)
                .HasForeignKey(i => i.PolozkaId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
