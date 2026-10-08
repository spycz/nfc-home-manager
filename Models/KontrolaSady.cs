namespace NfcHomeManager.Models;

// Zaznam o jedne provedene kontrole sady prvni pomoci (pruvodce kontrolou).
// Uklada stav zjisteny v okamziku kontroly, aby historie zustala citelna
// i po pozdejsim doplneni zasob.
public class KontrolaSady
{
    public int Id { get; set; }

    public int PolozkaId { get; set; }
    public Polozka? Polozka { get; set; }

    public DateOnly Datum { get; set; }

    public int Chybi { get; set; }
    public int Prosle { get; set; }
    public int BrzyExpiruje { get; set; }

    // Co pri kontrole chybelo nebo bylo prosle - text pro historii.
    public string? Souhrn { get; set; }
    public string? Poznamka { get; set; }

    public DateTime VytvorenoUtc { get; set; } = DateTime.UtcNow;

    [System.Text.Json.Serialization.JsonIgnore]
    public bool Uplna => Chybi == 0 && Prosle == 0;
}
