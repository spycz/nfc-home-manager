namespace NfcHomeManager.Models;

public enum ServisTyp
{
    Servis,
    Oprava,
    Kontrola,
    Jine
}

public class ServisniZaznam
{
    public int Id { get; set; }

    public int PolozkaId { get; set; }
    public Polozka? Polozka { get; set; }

    public DateOnly Datum { get; set; }
    public ServisTyp Typ { get; set; } = ServisTyp.Servis;
    public string Popis { get; set; } = string.Empty;
    public decimal? CenaKc { get; set; }
    public string? Provozovna { get; set; }

    // Pristi termin nastaveny timto zaznamem a jeho druh. Zustava v historii
    // i po pozdejsi zmene terminu; aktualni plan je v Polozka.Terminy.
    // U zaznamu z doby pred oddelenim terminu je druh neznamy (null).
    public DateOnly? DalsiTerminDo { get; set; }
    public TerminTyp? DalsiTerminTyp { get; set; }

    public DateTime VytvorenoUtc { get; set; } = DateTime.UtcNow;
}
