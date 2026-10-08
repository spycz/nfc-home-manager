using NfcHomeManager.Models;

namespace NfcHomeManager.Services;

public enum StavVybaveni
{
    VPoradku,
    BrzyExpiruje,
    Chybi,
    Prosle
}

// Vyhodnoceni vybaveni sady prvni pomoci proti cilove zasobe a expiracim.
// Sada je uplna, jen kdyz nic nechybi a nic neni prosle - kontrola sama
// o sobe neuplnou sadu "neopravi", stav se vzdy pocita ze skutecnych zasob.
public class StavSady
{
    // Vychozi hodnoty z roadmapy: kontrola kazdych 6 mesicu, upozorneni 60 dni pred expiraci.
    public const int MesicuDoDalsiKontroly = 6;
    public const int DniPredExpiraci = 60;

    // Poradi skupin v prehledech a krocich pruvodce; ostatni skupiny nasleduji abecedne.
    public static readonly string[] PoradiSkupin = ["Rány", "Obvazy", "Pomůcky", "Přípravky"];
    public const string SkupinaOstatni = "Ostatní";

    public List<Lek> Chybi { get; } = [];
    public List<Lek> Prosle { get; } = [];
    public List<Lek> BrzyExpiruje { get; } = [];
    public int Celkem { get; private init; }

    public bool Uplna => Chybi.Count == 0 && Prosle.Count == 0;

    public static StavSady Vyhodnotit(IEnumerable<Lek> vybaveni, DateOnly dnes)
    {
        var polozky = vybaveni.ToList();
        var stav = new StavSady { Celkem = polozky.Count };

        foreach (var polozka in polozky)
        {
            if (JeProsle(polozka, dnes)) stav.Prosle.Add(polozka);
            else if (BrzyExpirujeLek(polozka, dnes)) stav.BrzyExpiruje.Add(polozka);

            if (ChybiLek(polozka)) stav.Chybi.Add(polozka);
        }

        return stav;
    }

    public static StavVybaveni Vyhodnotit(Lek polozka, DateOnly dnes) =>
        JeProsle(polozka, dnes) ? StavVybaveni.Prosle
        : ChybiLek(polozka) ? StavVybaveni.Chybi
        : BrzyExpirujeLek(polozka, dnes) ? StavVybaveni.BrzyExpiruje
        : StavVybaveni.VPoradku;

    // Neznama zasoba (null) u polozky s cilem se pocita jako chybejici -
    // dokud ji nekdo nespocita, neni jiste, ze v sade je.
    private static bool ChybiLek(Lek polozka) =>
        polozka.CilovaZasoba is { } cil && cil > 0 && (polozka.Mnozstvi ?? 0) < cil;

    private static bool JeProsle(Lek polozka, DateOnly dnes) =>
        polozka.Expirace is { } expirace && expirace < dnes && (polozka.Mnozstvi ?? 1) > 0;

    private static bool BrzyExpirujeLek(Lek polozka, DateOnly dnes) =>
        polozka.Expirace is { } expirace && expirace >= dnes && expirace <= dnes.AddDays(DniPredExpiraci) && (polozka.Mnozstvi ?? 1) > 0;

    // Vybaveni rozdelene do skupin v poradi pro pruvodce.
    public static List<(string Skupina, List<Lek> Polozky)> PoSkupinach(IEnumerable<Lek> vybaveni) =>
        vybaveni
            .GroupBy(l => string.IsNullOrWhiteSpace(l.Skupina) ? SkupinaOstatni : l.Skupina.Trim())
            .OrderBy(g => Array.IndexOf(PoradiSkupin, g.Key) is var i and >= 0 ? i : g.Key == SkupinaOstatni ? int.MaxValue : PoradiSkupin.Length)
            .ThenBy(g => g.Key)
            .Select(g => (g.Key, g.OrderBy(l => l.Id).ToList()))
            .ToList();

    public string Souhrn()
    {
        static string Seznam(string nadpis, List<Lek> polozky) =>
            polozky.Count == 0 ? "" : $"{nadpis}: {string.Join(", ", polozky.Select(l => l.Nazev))}. ";

        return (Seznam("Chybí", Chybi) + Seznam("Prošlé", Prosle) + Seznam("Brzy expiruje", BrzyExpiruje)).Trim();
    }
}
