namespace NfcHomeManager.Models;

// Ručně potvrzená varianta včetně velikosti obchodního balení.
// Žádná data SÚKL ani odhad dávkování. Sdílí ji libovolný počet krabiček.
public class LekPripravek
{
    public int Id { get; set; }
    public string Klic { get; set; } = string.Empty;
    public string Nazev { get; set; } = string.Empty;
    public string? Sila { get; set; }
    public string? Forma { get; set; }
    public string? Ean { get; set; }
    public decimal? ObsahBaleni { get; set; }
    public string Jednotka { get; set; } = "tableta";
    public DateTime VytvorenoUtc { get; set; } = DateTime.UtcNow;
    public string Popisek => string.Join(" · ", new[] { Nazev, Sila, Forma,
        ObsahBaleni.HasValue ? $"{ObsahBaleni:0.###} {Jednotka}" : null }
        .Where(s => !string.IsNullOrWhiteSpace(s)));
}
