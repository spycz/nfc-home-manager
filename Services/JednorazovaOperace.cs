using Microsoft.EntityFrameworkCore;
using NfcHomeManager.Data;
using NfcHomeManager.Models;

namespace NfcHomeManager.Services;

// Zajisti, ze se zmena z jednoho odeslaneho formulare ulozi nejvys jednou.
// Pouziti v handleru:
//   if (await JednorazovaOperace.PresmerovaniAsync(db, operaceId, ct) is { } drive) return Redirect(drive);
//   ... zmeny v db ...
//   if (!await JednorazovaOperace.UlozitAsync(db, operaceId, url, ct)) return Redirect(...);
public static class JednorazovaOperace
{
    // Jak dlouho si pamatovat provedene operace (stare se mazou pri startu).
    public static readonly TimeSpan Uchovavat = TimeSpan.FromDays(30);

    // Byla-li operace uz provedena, vrati adresu, kam vedla; jinak null.
    public static async Task<string?> PresmerovaniAsync(AppDbContext db, Guid operaceId, CancellationToken ct) =>
        await db.ProvedeneOperace.AsNoTracking()
            .Where(o => o.Id == operaceId)
            .Select(o => o.Presmerovani)
            .FirstOrDefaultAsync(ct);

    // Ulozi rozpracovane zmeny spolu se zaznamem operace v jedne transakci.
    // Vrati false, pokud stejnou operaci mezitim ulozil soubezny pozadavek -
    // pak se zmeny tohoto pozadavku zahodi.
    public static async Task<bool> UlozitAsync(AppDbContext db, Guid operaceId, string presmerovani, CancellationToken ct)
    {
        db.ProvedeneOperace.Add(new ProvedenaOperace { Id = operaceId, Presmerovani = presmerovani });

        try
        {
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            if (await PresmerovaniAsync(db, operaceId, ct) is not null)
            {
                return false;
            }

            throw;
        }
    }
}
