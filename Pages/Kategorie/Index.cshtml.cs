using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NfcHomeManager.Data;
using KategorieModel = NfcHomeManager.Models.Kategorie;
using System.ComponentModel.DataAnnotations;

namespace NfcHomeManager.Pages.Kategorie;

public class IndexModel(AppDbContext db) : PageModel
{
    public List<KategorieModel> Kategorie { get; set; } = [];

    [BindProperty]
    [Required(ErrorMessage = "Zadej název kategorie.")]
    [StringLength(100)]
    public string NovyNazev { get; set; } = string.Empty;

    public async Task OnGetAsync(CancellationToken ct)
    {
        await NacistAsync(ct);
    }

    public async Task<IActionResult> OnPostPridatAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            await NacistAsync(ct);
            return Page();
        }

        db.Kategorie.Add(new KategorieModel { Nazev = NovyNazev.Trim() });
        await db.SaveChangesAsync(ct);
        return Redirect("/Kategorie");
    }

    public async Task<IActionResult> OnPostSmazatAsync(int id, CancellationToken ct)
    {
        var kategorie = await db.Kategorie.FindAsync([id], ct);
        if (kategorie is not null)
        {
            db.Kategorie.Remove(kategorie);
            await db.SaveChangesAsync(ct);
        }

        return Redirect("/Kategorie");
    }

    private async Task NacistAsync(CancellationToken ct)
    {
        Kategorie = await db.Kategorie
            .Include(k => k.Polozky)
            .OrderBy(k => k.Nazev)
            .ToListAsync(ct);
    }
}
