using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NfcHomeManager.Data;
using NfcHomeManager.Models;

namespace NfcHomeManager.Pages.P;

[AllowAnonymous]
public class IndexModel(AppDbContext db) : PageModel
{
    public Polozka? Polozka { get; set; }

    public async Task<IActionResult> OnGetAsync(string kod, CancellationToken ct)
    {
        Polozka = await db.Polozky
            .Include(p => p.Kategorie)
            .Include(p => p.Mistnost)
            .Include(p => p.ServisniZaznamy.OrderByDescending(s => s.Datum).Take(5))
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Kod == kod, ct);

        return Page();
    }
}
