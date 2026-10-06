using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using NfcHomeManager.Data;
using NfcHomeManager.Models;
using NfcHomeManager.Pages.Polozky;

namespace NfcHomeManager.Services;

// Serverova pravidla vztahu mezi polozkami. Formulare nabizeji jen povolene
// volby, ale POST lze sestavit rucne - proto se vse overuje znovu tady.
public static class PolozkaPravidla
{
    // Rezimy, ktere smi obsahovat jine polozky (Polozka.Obsah).
    public static bool MuzeMitObsah(NfcRezim rezim) => rezim is NfcRezim.Kontejner or NfcRezim.PrvniPomoc;

    // Vozidlo neni obecny kontejner, ale lze k nemu priradit sadu prvni
    // pomoci (autolekarnicka) - vazba pres stejne pole KontejnerId.
    public static bool JeVozidlo(NfcRezim rezim, Specializace specializace) =>
        rezim == NfcRezim.Predmet && specializace == Specializace.Auto;

    // Smi byt polozka daneho rezimu ulozena v/u rodice daneho druhu?
    public static bool SmiObsahovat(NfcRezim rodicRezim, Specializace rodicSpecializace, NfcRezim diteRezim) =>
        MuzeMitObsah(rodicRezim) || (JeVozidlo(rodicRezim, rodicSpecializace) && diteRezim == NfcRezim.PrvniPomoc);

    // Polozky nabizene ve vyberu "umisteno v": aktivni krabice, sady prvni
    // pomoci a vozidla. Archivovane se nenabizeji, krome te, ve ktere polozka
    // prave je (ponechatId) - jinak by ji formular pri ulozeni potichu vyjmul.
    public static Task<List<Polozka>> NabizeneKontejneryAsync(AppDbContext db, int? vlastniId, int? ponechatId, CancellationToken ct) =>
        db.Polozky.AsNoTracking()
            .Where(p => p.Id != vlastniId)
            .Where(p => p.Rezim == NfcRezim.Kontejner || p.Rezim == NfcRezim.PrvniPomoc ||
                        (p.Rezim == NfcRezim.Predmet && p.Specializace == Specializace.Auto))
            .Where(p => p.Aktivni || p.Id == ponechatId)
            .OrderBy(p => p.Nazev)
            .ToListAsync(ct);

    // Rezimy, ke kterym lze pridavat leky/prostredky (Polozka.Leky).
    public static bool MuzeMitLeky(NfcRezim rezim) => rezim is NfcRezim.Lekarnicka;

    // Nejvyssi povolene mnozstvi - shodne s [Range] ve formularich.
    public const decimal MaxMnozstvi = 1_000_000;

    // Overi formular nove nebo upravovane polozky. Pri uprave se predava jeji
    // Id, aby slo odhalit cyklus kontejneru a zmenu rezimu s existujicim obsahem.
    public static async Task ValidovatAsync(AppDbContext db, PolozkaFormInput input, int? vlastniId,
        ModelStateDictionary modelState, CancellationToken ct)
    {
        // Neplatne hodnoty vyctu (Rezim, Specializace) odmitne uz model binder
        // chybou v ModelState, tady se znovu neoveruji.
        if (input.KategorieId is { } kategorieId && !await db.Kategorie.AnyAsync(k => k.Id == kategorieId, ct))
        {
            modelState.AddModelError("Input.KategorieId", "Kategorie neexistuje.");
        }

        if (input.MistnostId is { } mistnostId && !await db.Mistnosti.AnyAsync(m => m.Id == mistnostId, ct))
        {
            modelState.AddModelError("Input.MistnostId", "Místnost neexistuje.");
        }

        if (input.KontejnerId is { } kontejnerId)
        {
            var chyba = await OveritKontejnerAsync(db, kontejnerId, vlastniId, input.Rezim, ct);
            if (chyba is not null)
            {
                modelState.AddModelError("Input.KontejnerId", chyba);
            }
        }

        if (vlastniId is { } id)
        {
            var rezimyObsahu = await db.Polozky.Where(p => p.KontejnerId == id).Select(p => p.Rezim).Distinct().ToListAsync(ct);
            if (rezimyObsahu.Any(dite => !SmiObsahovat(input.Rezim, input.Specializace, dite)))
            {
                modelState.AddModelError("Input.Rezim", "Položka má obsah. Nejdřív ho vyjmi nebo přesuň, pak změň druh nebo specializaci.");
            }

            if (!MuzeMitLeky(input.Rezim) && await db.Leky.AnyAsync(l => l.LekarnickaId == id, ct))
            {
                modelState.AddModelError("Input.Rezim", "Lékárnička obsahuje léky. Nejdřív je odeber, pak změň druh.");
            }
        }
    }

    // Vrati popis chyby, nebo null, kdyz smi byt polozka vlozena do kontejneru.
    private static async Task<string?> OveritKontejnerAsync(AppDbContext db, int kontejnerId, int? vlastniId, NfcRezim vlastniRezim, CancellationToken ct)
    {
        if (kontejnerId == vlastniId)
        {
            return "Položka nemůže být kontejnerem sama pro sebe.";
        }

        var kontejner = await db.Polozky.AsNoTracking()
            .Where(p => p.Id == kontejnerId)
            .Select(p => new { p.Rezim, p.Specializace, p.Aktivni })
            .FirstOrDefaultAsync(ct);

        if (kontejner is null)
        {
            return "Vybraná krabice neexistuje.";
        }

        if (!SmiObsahovat(kontejner.Rezim, kontejner.Specializace, vlastniRezim))
        {
            return JeVozidlo(kontejner.Rezim, kontejner.Specializace)
                ? "K vozidlu lze přiřadit jen sadu první pomoci."
                : "Do vybrané položky nelze vkládat obsah.";
        }

        // Do archivovane krabice nelze nic nove vlozit. Polozka, ktera v ni uz
        // je, v ni smi zustat - jinak by nesla ulozit ani nesouvisejici uprava.
        if (!kontejner.Aktivni)
        {
            var uzVNi = vlastniId is { } id &&
                await db.Polozky.AnyAsync(p => p.Id == id && p.KontejnerId == kontejnerId, ct);
            if (!uzVNi)
            {
                return "Vybraná krabice je archivovaná.";
            }
        }

        if (vlastniId is null)
        {
            return null;
        }

        // Projit retez rodicu od ciloveho kontejneru nahoru; narazime-li na
        // upravovanou polozku, vznikl by cyklus (A v B, B v A). Pocet kroku
        // je omezen pro pripad, ze uz v datech nejaky cyklus je.
        int? aktualni = kontejnerId;
        for (var krok = 0; aktualni is not null && krok < 1000; krok++)
        {
            if (aktualni == vlastniId)
            {
                return "Tím by vznikl cyklus: vybraná krabice je uložená uvnitř této položky.";
            }

            var dalsi = aktualni.Value;
            aktualni = await db.Polozky.AsNoTracking()
                .Where(p => p.Id == dalsi)
                .Select(p => p.KontejnerId)
                .FirstOrDefaultAsync(ct);
        }

        return null;
    }
}
