using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NfcHomeManager.Data;
using NfcHomeManager.Models;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace NfcHomeManager.Pages.Admin;

// Rucni import verejne databaze SUKL (DLP - Databaze lecivych pripravku,
// opendata.sukl.cz) pro dohledani nazvu leku podle EAN kodu pri skenovani.
// SUKL nema zive API klicovane EAN kodem, jen periodicke bulk exporty,
// takze se soubor stahne mimo appku a sem se rucne nahraje - viz README.
[RequestSizeLimit(100_000_000)]
public class ImportLekuModel(AppDbContext db) : PageModel
{
    public int PocetZaznamu { get; set; }
    public DateTime? PosledniImport { get; set; }
    public string? Vysledek { get; set; }
    public string? Chyba { get; set; }

    [BindProperty]
    public IFormFile? Soubor { get; set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        await NacistStavAsync(ct);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (Soubor is null || Soubor.Length == 0)
        {
            Chyba = "Vyber soubor.";
            await NacistStavAsync(ct);
            return Page();
        }

        List<LekovyKatalog> zaznamy;
        int celkemRadku;

        try
        {
            (zaznamy, celkemRadku) = await NacistSouborAsync(Soubor, ct);
        }
        catch (Exception ex)
        {
            Chyba = $"Soubor se nepodařilo zpracovat: {ex.Message}";
            await NacistStavAsync(ct);
            return Page();
        }

        if (zaznamy.Count == 0)
        {
            Chyba = "V souboru nebyl nalezen žádný záznam s vyplněným EAN kódem. " +
                    "Zkontroluj, že jde o export DLP ze SÚKL se sloupcem EAN.";
            await NacistStavAsync(ct);
            return Page();
        }

        // Plna nahrada - SUKL export se stahuje periodicky jako celek,
        // neni duvod slucovat se starymi daty.
        await db.LekovyKatalog.ExecuteDeleteAsync(ct);
        db.LekovyKatalog.AddRange(zaznamy);
        await db.SaveChangesAsync(ct);

        Vysledek = $"Naimportováno {zaznamy.Count} záznamů s EAN kódem (z {celkemRadku} řádků souboru).";
        await NacistStavAsync(ct);
        return Page();
    }

    private async Task NacistStavAsync(CancellationToken ct)
    {
        PocetZaznamu = await db.LekovyKatalog.CountAsync(ct);
        PosledniImport = await db.LekovyKatalog
            .OrderByDescending(l => l.NactenoUtc)
            .Select(l => (DateTime?)l.NactenoUtc)
            .FirstOrDefaultAsync(ct);
    }

    private static async Task<(List<LekovyKatalog> Zaznamy, int CelkemRadku)> NacistSouborAsync(IFormFile soubor, CancellationToken ct)
    {
        using var memory = new MemoryStream();
        await soubor.CopyToAsync(memory, ct);
        var text = DekodovatText(memory.ToArray());

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            DetectDelimiter = true,
            MissingFieldFound = null,
            BadDataFound = null,
            HeaderValidated = null
        };

        using var stringReader = new StringReader(text);
        using var csv = new CsvReader(stringReader, config);

        await csv.ReadAsync();
        csv.ReadHeader();

        var vysledek = new List<LekovyKatalog>();
        var celkemRadku = 0;
        var cas = DateTime.UtcNow;

        while (await csv.ReadAsync())
        {
            celkemRadku++;

            csv.TryGetField<string>("EAN", out var eanRaw);
            if (string.IsNullOrWhiteSpace(eanRaw))
            {
                continue;
            }

            csv.TryGetField<string>("NAZEV", out var nazev);
            csv.TryGetField<string>("SILA", out var sila);
            csv.TryGetField<string>("FORMA", out var forma);
            csv.TryGetField<string>("BALENI", out var baleni);
            csv.TryGetField<string>("ATC_WHO", out var atc);
            csv.TryGetField<string>("VYDEJ", out var vydej);
            csv.TryGetField<string>("KOD_SUKL", out var kodRaw);

            int? kodSukl = int.TryParse(kodRaw, out var kod) ? kod : null;

            foreach (var ean in RozdelitEany(eanRaw))
            {
                vysledek.Add(new LekovyKatalog
                {
                    Ean = ean,
                    KodSukl = kodSukl,
                    Nazev = nazev?.Trim() ?? string.Empty,
                    Sila = sila?.Trim(),
                    Forma = forma?.Trim(),
                    Baleni = baleni?.Trim(),
                    AtcWho = atc?.Trim(),
                    Vydej = vydej?.Trim(),
                    NactenoUtc = cas
                });
            }
        }

        return (vysledek, celkemRadku);
    }

    // Vetsinou jeden EAN na radek, ale nekdy je jich v poli vic oddelenych
    // carkou/strednikem/mezerou (napr. historicke prebaleni) - rozdelime
    // podle nenumerickych znaku a bereme jen rozumne dlouhe useky.
    private static IEnumerable<string> RozdelitEany(string raw)
    {
        foreach (var kus in Regex.Split(raw, @"\D+"))
        {
            if (kus.Length is >= 6 and <= 14)
            {
                yield return kus;
            }
        }
    }

    private static string DekodovatText(byte[] bytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            return Encoding.UTF8.GetString(bytes);
        }

        var utf8 = Encoding.UTF8.GetString(bytes);
        if (!utf8.Contains('�'))
        {
            return utf8;
        }

        // Nahradni znaky = spatne kodovani - starsi CZ vladni exporty
        // casto pouzivaji Windows-1250 misto UTF-8.
        return Encoding.GetEncoding(1250).GetString(bytes);
    }
}
