namespace NfcHomeManager.Models;

public class Polozka
{
    public int Id { get; set; }

    // Kratky kod v URL verejne stranky (/p/{Kod}) zapsane na NTAG215 stitek.
    public string Kod { get; set; } = string.Empty;

    // UID fyzickeho NFC cipu - volitelne, pro dohledani/duplicitu stitku.
    public string? NfcUid { get; set; }

    public string Nazev { get; set; } = string.Empty;

    public int? KategorieId { get; set; }
    public Kategorie? Kategorie { get; set; }

    public int? MistnostId { get; set; }
    public Mistnost? Mistnost { get; set; }

    public string? Vyrobce { get; set; }
    public string? Model { get; set; }
    public string? SerioveCislo { get; set; }

    public DateOnly? DatumPorizeni { get; set; }
    public decimal? CenaKc { get; set; }

    public int ZarukaMesice { get; set; } = 24;
    public DateOnly? ZarukaDo { get; set; }

    public DateOnly? DalsiServisDo { get; set; }

    public bool Aktivni { get; set; } = true;
    public string? Poznamka { get; set; }

    public DateTime VytvorenoUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpravenoUtc { get; set; } = DateTime.UtcNow;

    public List<ServisniZaznam> ServisniZaznamy { get; set; } = [];
    public List<Pojisteni> Pojisteni { get; set; } = [];

    public void PrepocitatZaruku()
    {
        ZarukaDo = DatumPorizeni?.AddMonths(ZarukaMesice);
    }
}
