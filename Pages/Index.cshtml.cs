using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NfcHomeManager.Data;
using NfcHomeManager.Services;

namespace NfcHomeManager.Pages;

public class IndexModel(AppDbContext db) : PageModel
{
    public int PocetPolozek { get; set; }
    public int PocetMistnosti { get; set; }
    public List<Upozorneni> Upozorneni { get; set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        PocetPolozek = await db.Polozky.CountAsync(p => p.Aktivni, ct);
        PocetMistnosti = await db.Mistnosti.CountAsync(ct);
        Upozorneni = await ReminderService.NacistAsync(db, ct: ct);
    }
}
