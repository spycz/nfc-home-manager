namespace NfcHomeManager.Models;

public class Kategorie
{
    public int Id { get; set; }
    public string Nazev { get; set; } = string.Empty;

    public List<Polozka> Polozky { get; set; } = [];
}
