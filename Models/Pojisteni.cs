namespace NfcHomeManager.Models;

public class Pojisteni
{
    public int Id { get; set; }

    public int PolozkaId { get; set; }
    public Polozka? Polozka { get; set; }

    public string Pojistovna { get; set; } = string.Empty;
    public string? CisloSmlouvy { get; set; }
    public string? Typ { get; set; }

    public DateOnly? PlatnostOd { get; set; }
    public DateOnly? PlatnostDo { get; set; }
    public decimal? RocniCenaKc { get; set; }
    public string? Poznamka { get; set; }

    public DateTime VytvorenoUtc { get; set; } = DateTime.UtcNow;
}
