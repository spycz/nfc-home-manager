using NfcHomeManager.Models;

namespace NfcHomeManager.Services;

// Vychozi obsah sady prvni pomoci. Jde o upravitelny navrh pro evidenci,
// ne o zdravotni ani pravni standard - viz ROADMAP, kapitola 10.
// Pripravky jsou jen obecne kategorie bez cilove zasoby (volitelne):
// konkretni nazev, silu a formu doplni uzivatel podle skutecne krabicky,
// vhodnost a davkovani se ridi pribalovym letakem, u ditete vekem a hmotnosti.
public record PolozkaSablony(string Skupina, string Nazev, decimal? Cil, string Jednotka, bool Obal = false, bool JeLek = false, string? Poznamka = null);

public record SablonaSady(string Klic, string Nazev, string Popis, PolozkaSablony[] Polozky);

public static class SablonySady
{
    private const string Rany = "Rány", Obvazy = "Obvazy", Pomucky = "Pomůcky", Pripravky = "Přípravky";

    private const string PodleKrabicky = "Doplň název, sílu a formu podle skutečné krabičky.";

    public static readonly SablonaSady[] Vse =
    [
        new("rodina-doma", "Doma – 2 dospělí a dítě 11–13 let",
            "Domácí sada pro drobná i větší poranění. Vybavení je společné; liší se jen velikosti a přípravky pro dítě.",
        [
            new(Rany, "Náplasti s polštářkem, různé velikosti", 30, "ks", Obal: true),
            new(Rany, "Náplasti na puchýře", 6, "ks", Obal: true),
            new(Rany, "Sterilní čtverce (malé a větší)", 10, "ks", Obal: true),
            new(Rany, "Nepřilnavé sterilní krytí", 4, "ks", Obal: true),
            new(Rany, "Náplast v roli", 1, "role"),
            new(Rany, "Stahovací proužky na ránu", 1, "balení", Obal: true),
            new(Rany, "Dezinfekce na drobné rány", 1, "balení", Poznamka: "Přípravek určený na kůži a rány podle označení výrobku; po otevření hlídej dobu použitelnosti."),

            new(Obvazy, "Hotový sterilní obvaz s polštářkem – střední", 2, "ks", Obal: true),
            new(Obvazy, "Hotový sterilní obvaz s polštářkem – velký", 2, "ks", Obal: true),
            new(Obvazy, "Fixační obinadlo", 3, "ks"),
            new(Obvazy, "Elastické obinadlo užší (dětská ruka, zápěstí)", 1, "ks"),
            new(Obvazy, "Elastické obinadlo širší (koleno, kotník dospělého)", 1, "ks"),
            new(Obvazy, "Trojcípý šátek", 2, "ks"),

            new(Pomucky, "Nitrilové rukavice (velikosti pro oba dospělé)", 4, "páry"),
            new(Pomucky, "Nůžky s tupou špičkou", 1, "ks"),
            new(Pomucky, "Pinzeta", 1, "ks"),
            new(Pomucky, "Pomůcka na odstranění klíštěte", 1, "ks"),
            new(Pomucky, "Teploměr", 1, "ks", Poznamka: "Při kontrole ověř baterii."),
            new(Pomucky, "Izotermická fólie", 2, "ks"),
            new(Pomucky, "Jednorázový chladicí sáček", 2, "ks"),
            new(Pomucky, "Resuscitační rouška s ventilem", 1, "ks", Obal: true),
            new(Pomucky, "Tištěný stručný návod první pomoci", 1, "ks"),
            new(Pomucky, "Karta s tísňovými čísly (155, 112) a údaji rodiny", 1, "ks", Poznamka: "Alergie, trvale užívané léky a kontakty – pro dospělé i dítě; při kontrole ověř aktuálnost."),

            new(Pripravky, "Lék proti bolesti a horečce – dospělí", null, "balení", JeLek: true, Poznamka: PodleKrabicky),
            new(Pripravky, "Lék proti bolesti a horečce – dítě 11–13 let", null, "balení", JeLek: true, Poznamka: PodleKrabicky + " Přípravek a dávka podle věku a hmotnosti dítěte dle příbalového letáku."),
            new(Pripravky, "Přípravek při alergické reakci", null, "balení", JeLek: true, Poznamka: PodleKrabicky + " Ověř, od jakého věku je určen."),
            new(Pripravky, "Gel na popáleniny a bodnutí hmyzem", null, "balení", Poznamka: PodleKrabicky)
        ]),

        new("vylety", "Výlety a sport – malá přenosná sada",
            "Menší samostatná sada do batohu nebo auta s vlastními zásobami.",
        [
            new(Rany, "Náplasti s polštářkem, různé velikosti", 10, "ks", Obal: true),
            new(Rany, "Náplasti na puchýře", 4, "ks", Obal: true),
            new(Rany, "Sterilní čtverce", 4, "ks", Obal: true),
            new(Rany, "Dezinfekce na drobné rány – malé balení", 1, "balení"),

            new(Obvazy, "Hotový sterilní obvaz s polštářkem", 2, "ks", Obal: true),
            new(Obvazy, "Fixační obinadlo", 1, "ks"),
            new(Obvazy, "Elastické obinadlo", 1, "ks"),
            new(Obvazy, "Trojcípý šátek", 1, "ks"),

            new(Pomucky, "Nitrilové rukavice", 2, "páry"),
            new(Pomucky, "Malé nůžky", 1, "ks"),
            new(Pomucky, "Pomůcka na odstranění klíštěte", 1, "ks"),
            new(Pomucky, "Izotermická fólie", 1, "ks"),
            new(Pomucky, "Karta s tísňovými čísly (155, 112) a údaji rodiny", 1, "ks"),

            new(Pripravky, "Lék proti bolesti a horečce – dospělí", null, "balení", JeLek: true, Poznamka: PodleKrabicky),
            new(Pripravky, "Lék proti bolesti a horečce – dítě 11–13 let", null, "balení", JeLek: true, Poznamka: PodleKrabicky + " Přípravek a dávka podle věku a hmotnosti dítěte dle příbalového letáku."),
            new(Pripravky, "Přípravek při alergické reakci", null, "balení", JeLek: true, Poznamka: PodleKrabicky + " Ověř, od jakého věku je určen.")
        ])
    ];

    public static SablonaSady? Najit(string? klic) => Vse.FirstOrDefault(s => s.Klic == klic);

    // Skutecna zasoba zustava neznama (null) - spocita se pri prvni kontrole.
    public static Lek Vytvorit(PolozkaSablony polozka, int sadaId) => new()
    {
        LekarnickaId = sadaId,
        Nazev = polozka.Nazev,
        Skupina = polozka.Skupina,
        CilovaZasoba = polozka.Cil,
        Jednotka = polozka.Jednotka,
        KontrolovatObal = polozka.Obal,
        JeLek = polozka.JeLek,
        Poznamka = polozka.Poznamka
    };
}
