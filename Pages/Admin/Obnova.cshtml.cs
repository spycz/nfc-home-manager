using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NfcHomeManager.Data;
using NfcHomeManager.Services;

namespace NfcHomeManager.Pages.Admin;

// Obnova databaze z JSON exportu ve dvou krocich: nahrani souboru s kontrolou
// a nahledem, potom vyslovne potvrzeni. Nahrany soubor mezi kroky ceka
// v docasne slozce pod nahodnym nazvem.
[RequestSizeLimit(MaxVelikost)]
public class ObnovaModel(AppDbContext db, ILogger<ObnovaModel> logger) : PageModel
{
    private const int MaxVelikost = 50_000_000;
    private static readonly TimeSpan PlatnostNahrani = TimeSpan.FromHours(1);

    [BindProperty]
    public IFormFile? Soubor { get; set; }

    public List<string> Chyby { get; set; } = [];
    public string? Vysledek { get; set; }

    // Nahled: nahrana zaloha a soucasny stav databaze.
    public Guid? NahraniId { get; set; }
    public ZalohaData? Nahrana { get; set; }
    public ZalohaData? Soucasna { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostNahratAsync(CancellationToken ct)
    {
        if (Soubor is null || Soubor.Length == 0)
        {
            Chyby.Add("Vyber soubor se zálohou.");
            return Page();
        }

        if (Soubor.Length > MaxVelikost)
        {
            Chyby.Add("Soubor je příliš velký.");
            return Page();
        }

        using var pamet = new MemoryStream();
        await Soubor.CopyToAsync(pamet, ct);
        var obsah = pamet.ToArray();

        var (data, chyby) = Zaloha.Nacist(obsah);
        if (data is null)
        {
            Chyby = chyby;
            return Page();
        }

        SmazatStaraNahrani();
        var id = Guid.NewGuid();
        await System.IO.File.WriteAllBytesAsync(CestaNahrani(id), obsah, ct);

        NahraniId = id;
        Nahrana = data;
        Soucasna = await Zaloha.VytvoritAsync(db, ct);
        return Page();
    }

    public async Task<IActionResult> OnPostObnovitAsync(Guid nahraniId, bool potvrzeno, CancellationToken ct)
    {
        var cesta = CestaNahrani(nahraniId);
        if (nahraniId == Guid.Empty || !System.IO.File.Exists(cesta))
        {
            Chyby.Add("Nahraný soubor už není k dispozici. Nahraj zálohu znovu.");
            return Page();
        }

        var (data, chyby) = Zaloha.Nacist(await System.IO.File.ReadAllBytesAsync(cesta, ct));
        if (data is null)
        {
            Chyby = chyby;
            return Page();
        }

        if (!potvrzeno)
        {
            Chyby.Add("Obnovu je potřeba potvrdit zaškrtnutím.");
            NahraniId = nahraniId;
            Nahrana = data;
            Soucasna = await Zaloha.VytvoritAsync(db, ct);
            return Page();
        }

        // Kopie soucasneho stavu pro pripad, ze obnova byla omyl.
        var kopie = DbInitializer.Zalohovat(db, "pred-obnovou");
        await Zaloha.ObnovitAsync(db, data, ct);
        System.IO.File.Delete(cesta);

        logger.LogWarning("Databáze obnovena ze zálohy z {Exportovano}; předchozí stav: {Kopie}", data.ExportovanoUtc, kopie);
        Vysledek = $"Databáze byla obnovena ze zálohy z {data.ExportovanoUtc.ToLocalTime():d.M.yyyy H:mm}. " +
                   $"Předchozí stav je uložený na serveru v souboru {Path.GetFileName(kopie)}.";
        return Page();
    }

    private static string SlozkaNahrani => Path.Combine(Path.GetTempPath(), "nfc-home-obnova");

    private static string CestaNahrani(Guid id) => Path.Combine(SlozkaNahrani, $"{id:N}.json");

    private static void SmazatStaraNahrani()
    {
        Directory.CreateDirectory(SlozkaNahrani);
        foreach (var soubor in Directory.EnumerateFiles(SlozkaNahrani, "*.json"))
        {
            if (DateTime.UtcNow - System.IO.File.GetLastWriteTimeUtc(soubor) > PlatnostNahrani)
            {
                System.IO.File.Delete(soubor);
            }
        }
    }
}
