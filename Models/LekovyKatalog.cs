namespace NfcHomeManager.Models;

// Jeden radek z verejne databaze SUKL (Databaze lecivych pripravku, DLP,
// opendata.sukl.cz), naimportovany rucne nahranym CSV/TSV souborem
// v /Admin/ImportLeku. Slouzi k dohledani nazvu leku podle EAN kodu
// z krabicky pri skenovani - viz /api/barcode v Program.cs.
public class LekovyKatalog
{
    public int Id { get; set; }

    public string Ean { get; set; } = string.Empty;
    public int? KodSukl { get; set; }
    public string Nazev { get; set; } = string.Empty;
    public string? Sila { get; set; }
    public string? Forma { get; set; }
    public string? Baleni { get; set; }
    public string? AtcWho { get; set; }

    // Kod zpusobu vydeje ze SUKL (napr. "R" = na predpis, "F" = volny vydej).
    public string? Vydej { get; set; }

    public DateTime NactenoUtc { get; set; } = DateTime.UtcNow;
}
