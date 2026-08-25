using Microsoft.EntityFrameworkCore;
using NfcHomeManager.Models;

namespace NfcHomeManager.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Mistnost> Mistnosti => Set<Mistnost>();
    public DbSet<Kategorie> Kategorie => Set<Kategorie>();
    public DbSet<Polozka> Polozky => Set<Polozka>();
    public DbSet<ServisniZaznam> ServisniZaznamy => Set<ServisniZaznam>();
    public DbSet<Pojisteni> Pojisteni => Set<Pojisteni>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Polozka>(entity =>
        {
            entity.HasIndex(p => p.Kod).IsUnique();
            entity.HasIndex(p => p.NfcUid).IsUnique().HasFilter("NfcUid IS NOT NULL");
            entity.Property(p => p.CenaKc).HasColumnType("decimal(18,2)");

            entity.HasOne(p => p.Kategorie)
                .WithMany(k => k.Polozky)
                .HasForeignKey(p => p.KategorieId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(p => p.Mistnost)
                .WithMany(m => m.Polozky)
                .HasForeignKey(p => p.MistnostId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ServisniZaznam>(entity =>
        {
            entity.Property(s => s.CenaKc).HasColumnType("decimal(18,2)");
            entity.Property(s => s.Typ).HasConversion<string>();

            entity.HasOne(s => s.Polozka)
                .WithMany(p => p.ServisniZaznamy)
                .HasForeignKey(s => s.PolozkaId)
                .OnDelete(DeleteBehavior.Cascade);
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
