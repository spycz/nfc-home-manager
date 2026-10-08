namespace NfcHomeManager.Models;

// Druh planovaneho terminu. Kazdy druh ma u polozky vlastni datum, takze
// zapsany servis neprepise termin STK ani revize.
public enum TerminTyp
{
    Servis,
    Stk,
    Revize,
    VymenaFiltru,
    Kontrola
}

// Pristi planovany termin jednoho druhu u polozky (nejvyse jeden od kazdeho druhu).
public class Termin
{
    public int Id { get; set; }

    public int PolozkaId { get; set; }
    public Polozka? Polozka { get; set; }

    public TerminTyp Typ { get; set; } = TerminTyp.Servis;
    public DateOnly DatumDo { get; set; }
    public string? Poznamka { get; set; }

    public DateTime VytvorenoUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpravenoUtc { get; set; } = DateTime.UtcNow;

    public static string Popis(TerminTyp typ) => typ switch
    {
        TerminTyp.Servis => "Servis",
        TerminTyp.Stk => "STK",
        TerminTyp.Revize => "Revize",
        TerminTyp.VymenaFiltru => "Výměna filtru",
        TerminTyp.Kontrola => "Kontrola",
        _ => typ.ToString()
    };

    // Ktery priznak Polozka.Sledovat* rozhoduje o pripominkach tohoto druhu.
    // Kontrola sady prvni pomoci se hlida vzdy - je to hlavni ucel sady.
    public static bool JeSledovany(TerminTyp typ, Polozka polozka) => typ switch
    {
        TerminTyp.Kontrola when polozka.Rezim == NfcRezim.PrvniPomoc => true,
        TerminTyp.Stk or TerminTyp.Revize => polozka.SledovatRevizi,
        _ => polozka.SledovatServis
    };
}
