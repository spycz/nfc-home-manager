using Microsoft.EntityFrameworkCore;
using NfcHomeManager.Models;

namespace NfcHomeManager.Data;

public static class DbInitializer
{
    public static void Initialize(AppDbContext context)
    {
        context.Database.EnsureCreated();
        DoplnitChybejiciSloupce(context);

        if (!context.Mistnosti.Any())
        {
            context.Mistnosti.AddRange(
                new Mistnost { Nazev = "Obývací pokoj" },
                new Mistnost { Nazev = "Kuchyň" },
                new Mistnost { Nazev = "Ložnice" },
                new Mistnost { Nazev = "Koupelna" },
                new Mistnost { Nazev = "Garáž" },
                new Mistnost { Nazev = "Dílna" },
                new Mistnost { Nazev = "Sklep" },
                new Mistnost { Nazev = "Zahrada" });
        }

        if (!context.Kategorie.Any())
        {
            context.Kategorie.AddRange(
                new Kategorie { Nazev = "Elektronika" },
                new Kategorie { Nazev = "Bílá technika" },
                new Kategorie { Nazev = "Nářadí" },
                new Kategorie { Nazev = "Nábytek" },
                new Kategorie { Nazev = "Auto / moto" },
                new Kategorie { Nazev = "Zahrada" },
                new Kategorie { Nazev = "Sport" },
                new Kategorie { Nazev = "Ostatní" });
        }

        context.SaveChanges();
    }

    // EnsureCreated zaklada schema jen u prazdne databaze a nove sloupce do
    // existujicich tabulek neprida. Dokud nejsou zavedene EF Core migrace,
    // doplni se tady jednorazove sloupce pridane po prvnim nasazeni.
    // Vychozi hodnoty odpovidaji bezpecnemu stavu (Verejna = 0 -> soukroma).
    private static void DoplnitChybejiciSloupce(AppDbContext context)
    {
        DoplnitSloupec(context, "Polozky", "Verejna", "INTEGER NOT NULL DEFAULT 0");
    }

    private static void DoplnitSloupec(AppDbContext context, string tabulka, string sloupec, string definice)
    {
        var existuje = context.Database
            .SqlQuery<int>($"SELECT COUNT(*) AS \"Value\" FROM pragma_table_info({tabulka}) WHERE name = {sloupec}")
            .AsEnumerable()
            .First() > 0;

        if (!existuje)
        {
            // Identifikatory nejdou predat jako SQL parametry; jsou to konstanty z kodu vyse.
            var sql = "ALTER TABLE \"" + tabulka + "\" ADD COLUMN \"" + sloupec + "\" " + definice;
            context.Database.ExecuteSqlRaw(sql);
        }
    }
}
