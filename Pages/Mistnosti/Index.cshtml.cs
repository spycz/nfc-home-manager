using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NfcHomeManager.Data;
using NfcHomeManager.Models;
using System.ComponentModel.DataAnnotations;

namespace NfcHomeManager.Pages.Mistnosti;

public class IndexModel(AppDbContext db) : PageModel
{
    public List<Mistnost> Mistnosti { get; set; } = [];

    [BindProperty]
    [Required(ErrorMessage = "Zadejte název místnosti.")]
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

        db.Mistnosti.Add(new Mistnost { Nazev = NovyNazev.Trim() });
        await db.SaveChangesAsync(ct);
        return Redirect("/Mistnosti");
    }

    public async Task<IActionResult> OnPostSmazatAsync(int id, CancellationToken ct)
    {
        var mistnost = await db.Mistnosti.FindAsync([id], ct);
        if (mistnost is not null)
        {
            db.Mistnosti.Remove(mistnost);
            await db.SaveChangesAsync(ct);
        }

        return Redirect("/Mistnosti");
    }

    private async Task NacistAsync(CancellationToken ct)
    {
        Mistnosti = await db.Mistnosti
            .Include(m => m.Polozky)
            .OrderBy(m => m.Nazev)
            .ToListAsync(ct);
    }
}
