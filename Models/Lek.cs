namespace NfcHomeManager.Models;

// Jedna polozka v lekarnicce (Polozka.Rezim == Lekarnicka): lek nebo
// zdravotnicky prostredek (naplast, obvaz...), ktery lekem neni, ale
// v evidenci lekarnicky/prvni pomoci ma byt taky.
public class Lek
{
    public int Id { get; set; }

    public int LekarnickaId { get; set; }
    public Polozka? Lekarnicka { get; set; }

    public string Nazev { get; set; } = string.Empty;
    public bool JeLek { get; set; } = true;

    // Carovy kod z krabicky - vyplni se rucne nebo naskenovanim kamerou.
    public string? Ean { get; set; }

    public decimal? Mnozstvi { get; set; }
    public string? Jednotka { get; set; }

    public string? NaCoJe { get; set; }
    public string? ProKoho { get; set; }
    public bool NaPredpis { get; set; }
    public string? Davkovani { get; set; }
    public string? NezadouciUcinky { get; set; }

    // S cim se nesmi kombinovat - jiny lek, alkohol apod.
    public string? Interakce { get; set; }

    public DateOnly? Expirace { get; set; }
    public string? Poznamka { get; set; }

    public DateTime VytvorenoUtc { get; set; } = DateTime.UtcNow;
}
