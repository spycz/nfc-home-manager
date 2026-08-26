namespace NfcHomeManager.Models;

// Co NFC stitek/polozka reprezentuje: obycejny predmet, nebo kontejner
// (krabice/mistnost) ci specialni kontejner na leky/prostredky prvni pomoci.
public enum NfcRezim
{
    Predmet,
    Kontejner,
    Lekarnicka,
    PrvniPomoc
}

// Specializovany profil predmetu - rozsiruje, co se u nej sleduje a jake
// popisky se pouzivaji (napr. STK/revize u auta a kotle).
public enum Specializace
{
    Obecna,
    Auto,
    PlynovyKotel
}

public class Polozka
{
    public int Id { get; set; }

    // Kratky kod v URL verejne stranky (/p/{Kod}) zapsane na NTAG215 stitek.
    public string Kod { get; set; } = string.Empty;

    // UID fyzickeho NFC cipu - volitelne, pro dohledani/duplicitu stitku.
    public string? NfcUid { get; set; }

    public string Nazev { get; set; } = string.Empty;

    public NfcRezim Rezim { get; set; } = NfcRezim.Predmet;
    public Specializace Specializace { get; set; } = Specializace.Obecna;

    // SPZ vozidla - relevantni jen pri Specializace == Auto.
    public string? Spz { get; set; }

    public int? KategorieId { get; set; }
    public Kategorie? Kategorie { get; set; }

    public int? MistnostId { get; set; }
    public Mistnost? Mistnost { get; set; }

    // Kontejner (krabice/mistnost s vlastni NFC), ve kterem je tato polozka
    // fyzicky ulozena. Nezavisi na tom, jestli ma polozka vlastni NFC kartu.
    public int? KontejnerId { get; set; }
    public Polozka? Kontejner { get; set; }
    public List<Polozka> Obsah { get; set; } = [];

    // Leky/prostredky evidovane pod touto polozkou, pokud Rezim == Lekarnicka.
    public List<Lek> Leky { get; set; } = [];

    public string? Vyrobce { get; set; }
    public string? Model { get; set; }
    public string? SerioveCislo { get; set; }

    public DateOnly? DatumPorizeni { get; set; }
    public decimal? CenaKc { get; set; }

    // Priznaky rikaji, jake vlastnosti se u teto konkretni polozky maji
    // sledovat/zobrazovat - napr. lampa nepotrebuje pojisteni ani revizi.
    public bool MaVlastniNfcKartu { get; set; } = true;
    public bool SledovatPojisteni { get; set; }
    public bool SledovatExpiraci { get; set; }
    public bool SledovatServis { get; set; }
    public bool SledovatRevizi { get; set; }

    // Obecne datum expirace (napr. baterie, chemie) - nezavisle na zaruce.
    public DateOnly? Expirace { get; set; }

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
