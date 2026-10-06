namespace NfcHomeManager.Models;

// Zaznam o jiz provedene zmene dat. Kazdy formular s nevratnou akci (odber
// mnozstvi, pridani zaznamu, zalozeni polozky) nese nahodne OperaceId; druhe
// odeslani stejneho formulare (dvojklik, F5, zpet + znovu odeslat, opakovani
// po vypadku site) se podle nej pozna a data nezmeni podruhe.
public class ProvedenaOperace
{
    public Guid Id { get; set; }

    // Kam presmerovat opakovane odeslani - stejne misto jako puvodni operace.
    public string Presmerovani { get; set; } = string.Empty;

    public DateTime VytvorenoUtc { get; set; } = DateTime.UtcNow;
}
