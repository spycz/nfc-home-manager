using Microsoft.EntityFrameworkCore;
using NfcHomeManager.Data;
using NfcHomeManager.Models;

namespace NfcHomeManager.Services;

public enum UpozorneniTyp
{
    Zaruka,
    Servis,
    Pojisteni,
    Expirace
}

public class Upozorneni
{
    public required Polozka Polozka { get; init; }
    public required UpozorneniTyp Typ { get; init; }
    public required DateOnly Datum { get; init; }
    public required string Popis { get; init; }
    public bool JeProsle => Datum < DateOnly.FromDateTime(DateTime.Today);
}

public static class ReminderService
{
    public static async Task<List<Upozorneni>> NacistAsync(AppDbContext db, int dnuDopredu = 60, CancellationToken ct = default)
    {
        var hranice = DateOnly.FromDateTime(DateTime.Today).AddDays(dnuDopredu);

        var polozky = await db.Polozky
            .Where(p => p.Aktivni)
            .Include(p => p.Pojisteni)
            .Include(p => p.Leky)
            .AsNoTracking()
            .ToListAsync(ct);

        var vysledek = new List<Upozorneni>();

        foreach (var p in polozky)
        {
            if (p.ZarukaDo is { } zarukaDo && zarukaDo <= hranice)
            {
                vysledek.Add(new Upozorneni { Polozka = p, Typ = UpozorneniTyp.Zaruka, Datum = zarukaDo, Popis = "Konec záruky" });
            }

            if (p.DalsiServisDo is { } dalsiServisDo && dalsiServisDo <= hranice)
            {
                vysledek.Add(new Upozorneni { Polozka = p, Typ = UpozorneniTyp.Servis, Datum = dalsiServisDo, Popis = "Plánovaný servis / STK" });
            }

            if (p.Expirace is { } expirace && expirace <= hranice)
            {
                vysledek.Add(new Upozorneni { Polozka = p, Typ = UpozorneniTyp.Expirace, Datum = expirace, Popis = "Expirace" });
            }

            // Polozka muze mit soucasne vic aktivnich pojisteni (napr. povinne
            // ruceni + havarijni u auta) - kazde se posuzuje samostatne, aby
            // driv konciciho pojisteni nezastinilo to s pozdejsim koncem.
            foreach (var pojisteni in p.Pojisteni)
            {
                if (pojisteni.PlatnostDo is { } platnostDo && platnostDo <= hranice)
                {
                    vysledek.Add(new Upozorneni
                    {
                        Polozka = p,
                        Typ = UpozorneniTyp.Pojisteni,
                        Datum = platnostDo,
                        Popis = $"Konec pojištění ({pojisteni.Pojistovna})"
                    });
                }
            }

            // Lekarnicka/prvni pomoc: kazdy lek/prostredek se posuzuje zvlast,
            // aby dřív expirující nezastinil ten s pozdejsim datem.
            foreach (var lek in p.Leky)
            {
                if (lek.Expirace is { } lekExpirace && lekExpirace <= hranice)
                {
                    vysledek.Add(new Upozorneni
                    {
                        Polozka = p,
                        Typ = UpozorneniTyp.Expirace,
                        Datum = lekExpirace,
                        Popis = $"Expirace: {lek.Nazev}" + (string.IsNullOrWhiteSpace(lek.ProKoho) ? "" : $" ({lek.ProKoho})")
                    });
                }
            }
        }

        return [.. vysledek.OrderBy(u => u.Datum)];
    }
}
