using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NfcHomeManager.Data;
using NfcHomeManager.Models;
using KategorieModel = NfcHomeManager.Models.Kategorie;

namespace NfcHomeManager.Pages.Polozky;

public class IndexModel(AppDbContext db) : PageModel
{
    public List<Polozka> Polozky { get; set; } = [];
    public List<KategorieModel> VsechnyKategorie { get; set; } = [];
    public List<Mistnost> VsechnyMistnosti { get; set; } = [];

    public string? Q { get; set; }
    public int? KategorieId { get; set; }
    public int? MistnostId { get; set; }
    public NfcRezim? Rezim { get; set; }
    public bool ZobrazitArchivovane { get; set; }

    public async Task OnGetAsync(string? q, int? kategorieId, int? mistnostId, NfcRezim? rezim, bool zobrazitArchivovane, CancellationToken ct)
    {
        Q = q;
        KategorieId = kategorieId;
        MistnostId = mistnostId;
        Rezim = rezim;
        ZobrazitArchivovane = zobrazitArchivovane;

        VsechnyKategorie = await db.Kategorie.OrderBy(k => k.Nazev).ToListAsync(ct);
        VsechnyMistnosti = await db.Mistnosti.OrderBy(m => m.Nazev).ToListAsync(ct);

        var query = db.Polozky
            .Include(p => p.Kategorie)
            .Include(p => p.Mistnost)
            .Include(p => p.Kontejner)
            .AsQueryable();

        if (!zobrazitArchivovane)
        {
            query = query.Where(p => p.Aktivni);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var like = $"%{q.Trim()}%";
            query = query.Where(p =>
                EF.Functions.Like(p.Nazev, like) ||
                (p.Vyrobce != null && EF.Functions.Like(p.Vyrobce, like)) ||
                (p.Model != null && EF.Functions.Like(p.Model, like)) ||
                (p.SerioveCislo != null && EF.Functions.Like(p.SerioveCislo, like)) ||
                (p.Ean != null && EF.Functions.Like(p.Ean, like)) ||
                EF.Functions.Like(p.Kod, like));
        }

        if (kategorieId.HasValue)
        {
            query = query.Where(p => p.KategorieId == kategorieId);
        }

        if (mistnostId.HasValue)
        {
            query = query.Where(p => p.MistnostId == mistnostId);
        }

        if (rezim.HasValue)
        {
            query = query.Where(p => p.Rezim == rezim);
        }

        Polozky = await query.OrderBy(p => p.Nazev).ToListAsync(ct);
    }
}
