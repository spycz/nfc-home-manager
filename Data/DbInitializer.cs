using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NfcHomeManager.Models;

namespace NfcHomeManager.Data;

public static class DbInitializer
{
    // Prvni migrace - odpovida schematu, ktere drive vytvarel EnsureCreated
    // (vcetne sloupce Polozky.Verejna). Databaze z doby pred migracemi se na
    // ni "prevezme" zapisem do historie, aniz by se tabulky zakladaly znovu.
    private const string VychoziMigrace = "20261005230036_VychoziSchema";

    // Tabulky a sloupce, ktere musi stara databaze mit, aby ji slo bezpecne
    // prevzit jako VychoziSchema. Odpovida CreateTable ve VychoziSchema.cs.
    private static readonly Dictionary<string, string[]> VychoziSchema = new()
    {
        ["Kategorie"] = ["Id", "Nazev"],
        ["Mistnosti"] = ["Id", "Nazev"],
        ["LekovyKatalog"] = ["Id", "Ean", "KodSukl", "Nazev", "Sila", "Forma", "Baleni", "AtcWho", "Vydej", "NactenoUtc"],
        ["Polozky"] =
        [
            "Id", "Kod", "NfcUid", "Nazev", "Rezim", "Specializace", "Spz", "KategorieId", "MistnostId", "KontejnerId",
            "Vyrobce", "Model", "SerioveCislo", "Ean", "Mnozstvi", "Jednotka", "DatumPorizeni", "CenaKc",
            "MaVlastniNfcKartu", "Verejna", "SledovatPojisteni", "SledovatExpiraci", "SledovatServis", "SledovatRevizi",
            "Expirace", "ZarukaMesice", "ZarukaDo", "DalsiServisDo", "Aktivni", "Poznamka", "VytvorenoUtc", "UpravenoUtc"
        ],
        ["Leky"] =
        [
            "Id", "LekarnickaId", "Nazev", "JeLek", "Ean", "Mnozstvi", "Jednotka", "NaCoJe", "ProKoho", "NaPredpis",
            "Davkovani", "NezadouciUcinky", "Interakce", "Expirace", "Poznamka", "VytvorenoUtc"
        ],
        ["Pojisteni"] = ["Id", "PolozkaId", "Pojistovna", "CisloSmlouvy", "Typ", "PlatnostOd", "PlatnostDo", "RocniCenaKc", "Poznamka", "VytvorenoUtc"],
        ["ServisniZaznamy"] = ["Id", "PolozkaId", "Datum", "Typ", "Popis", "CenaKc", "Provozovna", "DalsiTerminDo", "VytvorenoUtc"]
    };

    public static void Initialize(AppDbContext context, ILogger logger)
    {
        var staraDatabaze = TabulkaExistuje(context, "Polozky") && !TabulkaExistuje(context, "__EFMigrationsHistory");
        var cekajiciMigrace = context.Database.GetPendingMigrations().ToList();

        // Pred jakoukoli zmenou schematu existujici databaze udelat konzistentni
        // kopii (VACUUM INTO pocita i s daty jeste necheckpointovanymi z WAL).
        if (TabulkaExistuje(context, "Polozky") && (staraDatabaze || cekajiciMigrace.Count > 0))
        {
            var zaloha = Zalohovat(context);
            logger.LogInformation("Záloha databáze před migrací: {Zaloha}", zaloha);
        }

        if (staraDatabaze)
        {
            PrevzitStarouDatabazi(context);
            logger.LogInformation("Databáze z doby před migracemi převzata jako {Migrace}.", VychoziMigrace);
        }

        context.Database.Migrate();

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

    private static void PrevzitStarouDatabazi(AppDbContext context)
    {
        // Sloupec Verejna mohl chybet - pridaval ho primo DbInitializer jeste
        // pred zavedenim migraci. Ostatni sloupce musi existovat.
        var chybi = VychoziSchema
            .SelectMany(t => t.Value
                .Where(sloupec => !(t.Key == "Polozky" && sloupec == "Verejna"))
                .Where(sloupec => !SloupecExistuje(context, t.Key, sloupec))
                .Select(sloupec => $"{t.Key}.{sloupec}"))
            .ToList();

        if (chybi.Count > 0)
        {
            // Neprevzit naslepo - nasledne migrace by nad jinym schematem selhaly
            // nebo poskodily data. Databaze zustava beze zmeny.
            throw new InvalidOperationException(
                "Existující databáze neodpovídá výchozímu schématu, chybí: " + string.Join(", ", chybi));
        }

        // Vychozi 0 = polozka soukroma.
        if (!SloupecExistuje(context, "Polozky", "Verejna"))
        {
            context.Database.ExecuteSql($"ALTER TABLE \"Polozky\" ADD COLUMN \"Verejna\" INTEGER NOT NULL DEFAULT 0");
        }

        var historie = context.GetService<IHistoryRepository>();
        context.Database.ExecuteSqlRaw(historie.GetCreateIfNotExistsScript());
        context.Database.ExecuteSqlRaw(historie.GetInsertScript(new HistoryRow(VychoziMigrace, ProductInfo.GetVersion())));
    }

    private static string Zalohovat(AppDbContext context)
    {
        var zdroj = new SqliteConnectionStringBuilder(context.Database.GetConnectionString()).DataSource;
        var cesta = Path.GetFullPath($"{zdroj}.pred-migraci-{DateTime.UtcNow:yyyyMMdd-HHmmss}.bak");
        context.Database.ExecuteSql($"VACUUM INTO {cesta}");
        return cesta;
    }

    private static bool TabulkaExistuje(AppDbContext context, string tabulka) =>
        context.Database
            .SqlQuery<int>($"SELECT COUNT(*) AS \"Value\" FROM sqlite_master WHERE type = 'table' AND name = {tabulka}")
            .AsEnumerable()
            .First() > 0;

    private static bool SloupecExistuje(AppDbContext context, string tabulka, string sloupec) =>
        context.Database
            .SqlQuery<int>($"SELECT COUNT(*) AS \"Value\" FROM pragma_table_info({tabulka}) WHERE name = {sloupec}")
            .AsEnumerable()
            .First() > 0;
}
