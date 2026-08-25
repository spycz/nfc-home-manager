using NfcHomeManager.Models;

namespace NfcHomeManager.Data;

public static class DbInitializer
{
    public static void Initialize(AppDbContext context)
    {
        context.Database.EnsureCreated();

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
}
