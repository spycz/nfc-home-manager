using Microsoft.EntityFrameworkCore;
using NfcHomeManager.Data;
using NfcHomeManager.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NfcHomeManager.Services;

// Obsah JSON zalohy. Entity se serializuji jen s vlastnimi sloupci a cizimi
// klici (bez nactenych navigacnich vlastnosti), vcetne Id a kodu NFC stitku,
// aby po obnove dal fungovaly odkazy zapsane na fyzickych stitcich.
public class ZalohaData
{
    public int Verze { get; set; }
    public DateTime ExportovanoUtc { get; set; }
    public List<Mistnost> Mistnosti { get; set; } = [];
    public List<Kategorie> Kategorie { get; set; } = [];
    public List<Polozka> Polozky { get; set; } = [];
    public List<ServisniZaznam> ServisniZaznamy { get; set; } = [];
    public List<Termin> Terminy { get; set; } = [];
    public List<Pojisteni> Pojisteni { get; set; } = [];
    public List<Lek> Leky { get; set; } = [];
    public List<LekovyKatalog> LekovyKatalog { get; set; } = [];

    public IEnumerable<(string Nazev, int Pocet)> Pocty() =>
    [
        ("Místnosti", Mistnosti.Count),
        ("Kategorie", Kategorie.Count),
        ("Položky", Polozky.Count),
        ("Servisní záznamy", ServisniZaznamy.Count),
        ("Plánované termíny", Terminy.Count),
        ("Pojištění", Pojisteni.Count),
        ("Léky a prostředky", Leky.Count),
        ("Katalog léků SÚKL", LekovyKatalog.Count)
    ];
}

public static class Zaloha
{
    // Verze 1: export z doby pred oddelenim terminu - bez pole Verze, bez
    //          tabulky Terminy a katalogu, s Polozky[].DalsiServisDo.
    // Verze 2: soucasny format.
    public const int AktualniVerze = 2;

    // Vycty se zapisuji textem; pri cteni se prijmou i cisla ze starsich exportu.
    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static async Task<ZalohaData> VytvoritAsync(AppDbContext db, CancellationToken ct) => new()
    {
        Verze = AktualniVerze,
        ExportovanoUtc = DateTime.UtcNow,
        Mistnosti = await db.Mistnosti.AsNoTracking().OrderBy(x => x.Id).ToListAsync(ct),
        Kategorie = await db.Kategorie.AsNoTracking().OrderBy(x => x.Id).ToListAsync(ct),
        Polozky = await db.Polozky.AsNoTracking().OrderBy(x => x.Id).ToListAsync(ct),
        ServisniZaznamy = await db.ServisniZaznamy.AsNoTracking().OrderBy(x => x.Id).ToListAsync(ct),
        Terminy = await db.Terminy.AsNoTracking().OrderBy(x => x.Id).ToListAsync(ct),
        Pojisteni = await db.Pojisteni.AsNoTracking().OrderBy(x => x.Id).ToListAsync(ct),
        Leky = await db.Leky.AsNoTracking().OrderBy(x => x.Id).ToListAsync(ct),
        LekovyKatalog = await db.LekovyKatalog.AsNoTracking().OrderBy(x => x.Id).ToListAsync(ct)
    };

    public static byte[] Serializovat(ZalohaData data) => JsonSerializer.SerializeToUtf8Bytes(data, Json);

    // Nacte a overi soubor zalohy. Vrati data, nebo seznam chyb (pak jsou data null).
    public static (ZalohaData? Data, List<string> Chyby) Nacist(byte[] obsah)
    {
        ZalohaData? data;
        try
        {
            using var dokument = JsonDocument.Parse(obsah);
            var koren = dokument.RootElement;
            if (koren.ValueKind != JsonValueKind.Object || !koren.TryGetProperty(nameof(ZalohaData.Polozky), out _))
            {
                return (null, ["Soubor není export této aplikace (chybí seznam položek)."]);
            }

            var verze = koren.TryGetProperty(nameof(ZalohaData.Verze), out var v) && v.TryGetInt32(out var cislo) ? cislo : 1;
            if (verze is < 1 or > AktualniVerze)
            {
                return (null, [$"Záloha má verzi {verze}, tato aplikace umí nejvýš verzi {AktualniVerze}. Aktualizuj aplikaci."]);
            }

            data = koren.Deserialize<ZalohaData>(Json);
            if (data is null)
            {
                return (null, ["Soubor je prázdný."]);
            }

            data.Verze = verze;
            if (verze == 1)
            {
                PrevestTerminyZVerze1(koren, data);
            }
        }
        catch (JsonException ex)
        {
            return (null, [$"Soubor není platný JSON export: {ex.Message}"]);
        }

        // Navigacni vlastnosti nesmi do databaze nic pridat - plati jen cizi klice.
        data.Mistnosti.ForEach(m => m.Polozky = []);
        data.Kategorie.ForEach(k => k.Polozky = []);
        foreach (var p in data.Polozky)
        {
            (p.Kategorie, p.Mistnost, p.Kontejner) = (null, null, null);
            (p.Obsah, p.Leky, p.ServisniZaznamy, p.Pojisteni, p.Terminy) = ([], [], [], [], []);
        }

        data.ServisniZaznamy.ForEach(s => s.Polozka = null);
        data.Terminy.ForEach(t => t.Polozka = null);
        data.Pojisteni.ForEach(i => i.Polozka = null);
        data.Leky.ForEach(l => l.Lekarnicka = null);

        var chyby = Overit(data);
        return chyby.Count == 0 ? (data, chyby) : (null, chyby);
    }

    // Stejne pravidlo jako migrace OddeleneTerminy: spolecne pole "dalsi
    // servis / STK" se stane terminem jednoho druhu podle sledovanych priznaku.
    private static void PrevestTerminyZVerze1(JsonElement koren, ZalohaData data)
    {
        var podleId = data.Polozky.ToDictionary(p => p.Id);
        var dalsiId = 1;

        foreach (var prvek in koren.GetProperty(nameof(ZalohaData.Polozky)).EnumerateArray())
        {
            if (!prvek.TryGetProperty("DalsiServisDo", out var datum) || datum.ValueKind != JsonValueKind.String ||
                !DateOnly.TryParse(datum.GetString(), out var datumDo) ||
                !prvek.TryGetProperty(nameof(Polozka.Id), out var idPrvek) || !podleId.TryGetValue(idPrvek.GetInt32(), out var polozka))
            {
                continue;
            }

            var jenRevize = !polozka.SledovatServis && polozka.SledovatRevizi;
            data.Terminy.Add(new Termin
            {
                Id = dalsiId++,
                PolozkaId = polozka.Id,
                Typ = !jenRevize ? TerminTyp.Servis : polozka.Specializace == Specializace.Auto ? TerminTyp.Stk : TerminTyp.Revize,
                DatumDo = datumDo,
                Poznamka = polozka.SledovatServis && polozka.SledovatRevizi
                    ? "Převedeno ze společného pole „servis / STK“ – ověř druh."
                    : null
            });
        }
    }

    private static List<string> Overit(ZalohaData data)
    {
        var chyby = new List<string>();

        void Duplicity<T>(string tabulka, IEnumerable<T> radky, Func<T, int> id)
        {
            var opakovana = radky.GroupBy(id).Where(g => g.Count() > 1 || g.Key <= 0).Select(g => g.Key).Take(5).ToList();
            if (opakovana.Count > 0)
            {
                chyby.Add($"{tabulka}: neplatné nebo opakované Id {string.Join(", ", opakovana)}.");
            }
        }

        Duplicity("Místnosti", data.Mistnosti, x => x.Id);
        Duplicity("Kategorie", data.Kategorie, x => x.Id);
        Duplicity("Položky", data.Polozky, x => x.Id);
        Duplicity("Servisní záznamy", data.ServisniZaznamy, x => x.Id);
        Duplicity("Termíny", data.Terminy, x => x.Id);
        Duplicity("Pojištění", data.Pojisteni, x => x.Id);
        Duplicity("Léky", data.Leky, x => x.Id);
        Duplicity("Katalog léků", data.LekovyKatalog, x => x.Id);

        var mistnosti = data.Mistnosti.Select(x => x.Id).ToHashSet();
        var kategorie = data.Kategorie.Select(x => x.Id).ToHashSet();
        var polozky = data.Polozky.GroupBy(p => p.Id).ToDictionary(g => g.Key, g => g.First());

        foreach (var p in data.Polozky)
        {
            var kdo = $"Položka {p.Id} („{p.Nazev}“)";
            if (string.IsNullOrWhiteSpace(p.Nazev)) chyby.Add($"Položka {p.Id}: chybí název.");
            if (string.IsNullOrWhiteSpace(p.Kod)) chyby.Add($"{kdo}: chybí kód NFC štítku.");
            if (!Enum.IsDefined(p.Rezim) || !Enum.IsDefined(p.Specializace)) chyby.Add($"{kdo}: neznámý druh nebo specializace.");
            if (p.MistnostId is { } m && !mistnosti.Contains(m)) chyby.Add($"{kdo}: odkazuje na neexistující místnost {m}.");
            if (p.KategorieId is { } k && !kategorie.Contains(k)) chyby.Add($"{kdo}: odkazuje na neexistující kategorii {k}.");
            if (p.KontejnerId is { } kontejner && !polozky.ContainsKey(kontejner)) chyby.Add($"{kdo}: odkazuje na neexistující krabici {kontejner}.");

            // Cyklus krabic: jit po rodicich nahoru, nejvys tolikrat, kolik je polozek.
            var aktualni = p.KontejnerId;
            for (var krok = 0; aktualni is { } rodic && polozky.TryGetValue(rodic, out var dalsi); krok++)
            {
                if (rodic == p.Id || krok > polozky.Count)
                {
                    chyby.Add($"{kdo}: je součástí cyklu krabic.");
                    break;
                }

                aktualni = dalsi.KontejnerId;
            }
        }

        foreach (var kod in data.Polozky.Where(p => !string.IsNullOrWhiteSpace(p.Kod)).GroupBy(p => p.Kod).Where(g => g.Count() > 1))
        {
            chyby.Add($"Kód NFC štítku {kod.Key} je v záloze u více položek.");
        }

        foreach (var uid in data.Polozky.Where(p => p.NfcUid is not null).GroupBy(p => p.NfcUid).Where(g => g.Count() > 1))
        {
            chyby.Add($"UID NFC štítku {uid.Key} je v záloze u více položek.");
        }

        void Vazba(string tabulka, IEnumerable<(int Id, int PolozkaId)> radky)
        {
            foreach (var (id, polozkaId) in radky.Where(r => !polozky.ContainsKey(r.PolozkaId)))
            {
                chyby.Add($"{tabulka} {id}: odkazuje na neexistující položku {polozkaId}.");
            }
        }

        Vazba("Servisní záznam", data.ServisniZaznamy.Select(s => (s.Id, s.PolozkaId)));
        Vazba("Termín", data.Terminy.Select(t => (t.Id, t.PolozkaId)));
        Vazba("Pojištění", data.Pojisteni.Select(i => (i.Id, i.PolozkaId)));
        Vazba("Lék", data.Leky.Select(l => (l.Id, l.LekarnickaId)));

        if (data.ServisniZaznamy.Any(s => !Enum.IsDefined(s.Typ) || (s.DalsiTerminTyp is { } t && !Enum.IsDefined(t))))
        {
            chyby.Add("Servisní záznamy: neznámý typ záznamu nebo termínu.");
        }

        if (data.Terminy.Any(t => !Enum.IsDefined(t.Typ)))
        {
            chyby.Add("Termíny: neznámý druh termínu.");
        }

        foreach (var dvojice in data.Terminy.GroupBy(t => (t.PolozkaId, t.Typ)).Where(g => g.Count() > 1))
        {
            chyby.Add($"Položka {dvojice.Key.PolozkaId}: více termínů druhu {Termin.Popis(dvojice.Key.Typ)}.");
        }

        const int maxChyb = 20;
        if (chyby.Count > maxChyb)
        {
            var dalsich = chyby.Count - maxChyb;
            chyby = [.. chyby.Take(maxChyb), $"… a dalších {dalsich} chyb."];
        }

        return chyby;
    }

    // Nahradi cely obsah databaze daty ze zalohy. Bezi v jedne transakci:
    // pri jakekoli chybe zustane databaze v puvodnim stavu.
    public static async Task ObnovitAsync(AppDbContext db, ZalohaData data, CancellationToken ct)
    {
        db.ChangeTracker.Clear();
        await using var transakce = await db.Database.BeginTransactionAsync(ct);

        await db.Leky.ExecuteDeleteAsync(ct);
        await db.ServisniZaznamy.ExecuteDeleteAsync(ct);
        await db.Terminy.ExecuteDeleteAsync(ct);
        await db.Pojisteni.ExecuteDeleteAsync(ct);
        await db.Polozky.ExecuteDeleteAsync(ct);
        await db.Mistnosti.ExecuteDeleteAsync(ct);
        await db.Kategorie.ExecuteDeleteAsync(ct);
        await db.LekovyKatalog.ExecuteDeleteAsync(ct);

        // Polozky nejdriv bez vazby na krabici, ta se doplni az existuji vsechny.
        var kontejnery = data.Polozky.Where(p => p.KontejnerId is not null).ToDictionary(p => p.Id, p => p.KontejnerId);
        data.Polozky.ForEach(p => p.KontejnerId = null);

        db.Mistnosti.AddRange(data.Mistnosti);
        db.Kategorie.AddRange(data.Kategorie);
        db.Polozky.AddRange(data.Polozky);
        db.LekovyKatalog.AddRange(data.LekovyKatalog);
        await db.SaveChangesAsync(ct);

        foreach (var p in data.Polozky.Where(p => kontejnery.ContainsKey(p.Id)))
        {
            p.KontejnerId = kontejnery[p.Id];
        }

        db.ServisniZaznamy.AddRange(data.ServisniZaznamy);
        db.Terminy.AddRange(data.Terminy);
        db.Pojisteni.AddRange(data.Pojisteni);
        db.Leky.AddRange(data.Leky);
        await db.SaveChangesAsync(ct);

        await transakce.CommitAsync(ct);
        db.ChangeTracker.Clear();
    }
}
