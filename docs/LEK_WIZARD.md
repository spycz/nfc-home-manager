# Průvodce přidáním léku a návrh datového modelu

Návrh z 18. 9. 2026. Navazuje na kapitolu 10 [roadmapy](../ROADMAP.md). Následující kapitoly popisují cílovou specifikaci. Rozsah první implementace je uveden níže.

## První implementace bez importu (18. 9. 2026)

- Stránka `/Leky/Pridat` nabízí pět kroků s ručním názvem, silou, formou, obsahem balení a volitelným EAN/GTIN. Bez JavaScriptu je dostupný celý formulář. Kamera je volitelná a nepoužívá externí lookup.
- `LekPripravek` prozatím spojuje přípravek a obchodní velikost balení do jedné ručně potvrzené varianty. `Lek` představuje samostatnou krabičku s volitelnou vazbou na variantu. Plné rozdělení katalogu z kapitoly 5 zůstává budoucím návrhem.
- Uloženou variantu lze vybrat z vlastní evidence. Krabička má zůstatek (nebo explicitně neznámý), vlastní datum expirace (nebo k doplnění), šarži, datum otevření, poznámku a přepínač upozornění. Přidání i úprava fungují v Lékárničce i První pomoci.
- Opakované odeslání přidání používá unikátní ID operace. Úpravy a rychlá změna množství kontrolují verzi krabičky, takže zastaralý formulář nepřepíše novější zůstatek.
- Existující řádky nejsou automaticky slučovány ani přejmenovány. Při úpravě lze ručně vytvořit/přiřadit přípravek. Původní zdravotní poznámky zůstávají zachované.
- `LekSchemaUpgrade` je úzký aditivní upgrade existujícího schématu vytvořeného přes EnsureCreated. Před změnou vytvoří konzistentní SQLite zálohu vedle databáze (včetně WAL), poté přidá tabulku/sloupce/indexy v transakci. Selhání zálohy zastaví upgrade. Není to obecný systém EF migrací.
- JSON export má verzi 2 a obsahuje také ruční přípravky. Upozornění respektují přepínač balení a nulový zůstatek. Sadu s evidovanými léky/prostředky nelze smazat kaskádově přes detail.
- Dosud není implementován import SÚKL/PIL, účinné látky, odborný popis použití, historie pohybů, automatická použitelnost po otevření, zadání expirace pouze měsíc/rok ani samostatné NFC krabičky. Datum expirace se neodvozuje od záruky. Dosavadní importní stránka zůstává oddělená a wizard ji nepoužívá.

Před nasazením zastavit zápisy ostatních instancí a uchovat zálohu mimo server. Ověřit upgrade na kopii vlastní databáze. Obnova spočívá ve vrácení původní databáze při zastavené aplikaci a použití odpovídající původní verze aplikace; nové záznamy vzniklé po upgradu tím budou ztraceny.

Ověření: `dotnet build NfcHomeManager.csproj` a `dotnet run --project tests/LekWizard.Smoke/LekWizard.Smoke.csproj`. Smoke testy používají dočasnou SQLite, kontrolují zachování původních dat a čitelnost zálohy, opakování upgradu, sdílení přípravku, oddělené expirace, idempotenci přidání, neplatné rodiče a souběžné změny. GitHub workflow provádí stejné kontroly. Kamera a skutečné mobilní ovládání vyžadují ruční kontrolu na HTTPS.

## 1. Ověřený současný stav

Porovnání vychází ze zdrojového kódu na commitu `97a2e96cd5cda7639a99bc8627357f8ce167058c`, nikoli z inspekce provozního souboru SQLite.

- [Polozka](../Models/Polozka.cs) je obecný předmět nebo kontejner. Režimy: Predmet, Kontejner, Lekarnicka, PrvniPomoc. Specializace: Obecna, Auto, PlynovyKotel; specializace Lek neexistuje.
- [Lek](../Models/Lek.cs) je samostatná entita v `Leky`, nikoli podtyp Polozka. Přes `LekarnickaId` patří k položce. Obsahuje zároveň popis přípravku a údaje zásoby.
- [LekovyKatalog](../Models/LekovyKatalog.cs) existuje, ale Lek na něj nemá cizí klíč.
- [AppDbContext](../Data/AppDbContext.cs) mapuje uvedené entity. Smazání lékárničky dnes kaskádově maže její léky. EAN katalogu má neunikátní index.
- [Detail.cshtml](../Pages/Polozky/Detail.cshtml) nabízí dlouhý formulář pouze pro režim Lekarnicka; PrvniPomoc používá obecný obsah Polozka. Serverový handler přidání léku ověřuje existenci rodiče, ale ne jeho povolený režim.
- [Detail.cshtml.cs](../Pages/Polozky/Detail.cshtml.cs) umí přidat, smazat a změnit množství léku; neobsahuje handler jeho plné editace. Změna množství nemá historii pohybů.
- [Program.cs](../Program.cs): lookup čárového kódu hledá vlastní léky, obecné položky, první shodu katalogu a poté Open Food Facts. Vrací jméno/značku, nikoli identitu přípravku a strukturovaná data.
- [ImportLeku.cshtml.cs](../Pages/Admin/ImportLeku.cshtml.cs) čte CSV/TSV přes CsvHelper, očekává pole EAN, NAZEV atd., přeskakuje záznamy bez EAN a katalog plně nahrazuje. XLSX nečte.
- [DbInitializer](../Data/DbInitializer.cs) používá EnsureCreated, nikoli správu změn existujícího schématu migracemi.

Dodaný XLSX s 12 variantami Paralenu nemá EAN ani velikost balení. Pouhý převod do CSV problém nevyřeší: je nutné mapovat hlavičky, dovolit záznamy bez EAN a doplnit hledání podle názvu.

## 2. Rozhodnutí: co znamená „položka je lék“

V rozhraní může uživatel vnímat lék jako položku inventáře. V databázi navrhuji pro první implementaci zachovat samostatnou evidenci léčiv a rozdělit ji na:

1. **Přípravek:** název, síla, forma a odborné údaje.
2. **Produktové balení:** například konkrétní velikost krabičky a její EAN.
3. **Fyzické balení:** jedna skutečná krabička se zůstatkem a expirací.
4. **Umístění:** lékárnička nebo sada první pomoci, stále reprezentovaná Polozka.

Není potřeba kopírovat jednu krabičku zároveň do Polozky a Leky. Pro jednotný seznam lze připravit společnou projekci pro UI. Pokud se později všechny věci sjednotí pod Polozka, lékové údaje budou samostatným rozšířením 1:1; to je větší migrace, nikoli podmínka tohoto průvodce.

## 3. Průvodce: pět krátkých kroků

Vstup: **Lékárnička → Přidat → Lék** nebo stejná akce ze sady první pomoci. Cílová sada je předvyplněná. Pro obvaz či jiný materiál zvolit větev Prostředek bez katalogu léčiv.

| Krok a text obrazovky | Co zadává uživatel | Co dělá aplikace | Podmínka pokračování |
|---|---|---|---|
| 1. Naskenuj krabičku | Sken EAN, ruční kód nebo „Bez kódu“; název podle krabičky | U známého kódu nabídne uloženou vazbu, jinak hledání podle názvu | Kód je volitelný; musí být zvolen známý produkt nebo zadán název pro další hledání |
| 2. Vyber přesný přípravek | Potvrdí název, sílu, formu a velikost balení | Nabídne kandidáty, účinné látky a zdroj; údaje balení oddělí od registrace | Potvrzená varianta nebo vědomá volba „Zadat ručně“ |
| 3. Tvoje krabička | Zůstatek, jednotka, expirace; volitelně šarže a otevření | Vytvoří návrh jednoho fyzického balení, nepřebírá expiraci z katalogu | Platné množství a jednotka; expirace nebo výslovné „Nevím, doplním později“ |
| 4. Kam patří a co sledovat | Sada, sledování expirace, volitelná poznámka | Předvyplní výchozí sadu a upozornění; ukáže případný PIL a zdrojovaný popis | Povolené aktivní umístění, platné nastavení |
| 5. Zkontroluj a ulož | Potvrdí souhrn; „Uložit“ nebo „Uložit a přidat další“ | Jednou transakcí zapíše potřebné vazby, balení a počáteční pohyb | Server znovu ověří celé zadání a jedinečnost operace |

Každý krok má Zpět, zachovává rozepsané hodnoty a ukazuje chyby u pole i v souhrnu. Zrušení před závěrečným potvrzením nevytváří produkt ani zásobu. Bez kamery funguje ruční vstup. Výpadek katalogu neblokuje ruční evidenci.

### Výběr přípravku

- Výsledek zobrazit jako „PARALEN · 500MG · Tableta“. V ukázkovém exportu existuje také „PARALEN · 500MG · Čípek“, proto nesmí rozhodovat pouze název a síla.
- U každého výsledku rozlišit import SÚKL, vlastní potvrzený produkt a ruční záznam. Potvrzení uživatelem znamená spárování krabičky, nikoli oficiální ověření léčiva.
- Pokud import neobsahuje velikost balení, zapsat ji z obalu. Velikost krabičky a aktuální zůstatek jsou různá pole.
- U nejednoznačného EAN nenabírat první databázový řádek. Nabídnout kandidáty a zaznamenat potvrzení.
- Změna přípravku zneplatní dříve odvozené údaje a vazbu k PIL; ručně zadané údaje krabičky zachovat pouze po kontrole jednotek.
- Při ručním založení stačí název; neznámá síla, forma nebo velikost jsou NULL s příznakem neúplného záznamu. Pozdější doplnění nesmí přepsat historii zásob.
- V lékovém průvodci nepoužívat Open Food Facts jako zdroj identifikace či odborných údajů.

### Evidence krabičky a rychlé opakování

Výchozí režim je **jedna skutečná krabička = jeden záznam**. Stejný EAN může mít mnoho fyzických balení. Další balení se stejnou expirací lze přidat opakováním s potvrzením, nikoli sloučením do jediné krabičky.

Pro tablety se volí jednotka tableta, pro tekutiny například ml; pokud uživatel nechce sledovat obsah, může evidovat celé balení. Převod z „balení“ na „tablety“ nesmí proběhnout bez známé velikosti a potvrzení. Neznámý zůstatek je NULL, nikoli nula. Počáteční plný obsah lze nabídnout, ale uživatel jej potvrdí.

Expirace umožní datum nebo měsíc/rok a uloží také přesnost původního údaje. Výpočet rozhodného dne pro připomínky musí mít explicitní pravidlo podle označení výrobku; nezobrazovat vymyšlený přesný den jako převzatý z krabičky. Datum otevření a případný termín po otevření se evidují odděleně se zdrojem pravidla.

„Uložit a přidat další“ zachová umístění a nastavení sledování, vymaže kód, výběr přípravku, množství, expiraci a šarži. Samostatná volba „Další krabička stejného přípravku“ zachová produkt, ale znovu vyžádá údaje balení.

## 4. Porovnání existujících polí a cílového stavu

| Informace | Polozka dnes | Lek / katalog dnes | Cílový stav pro lék |
|---|---|---|---|
| Identita | Id, Kod | Lek.Id; katalog.Id bez vazby | Interní ID balení a cizí klíče na produkt/přípravek; EAN není ID krabičky |
| Název | Nazev | Obě entity Nazev | Oficiální/ruční název přípravku, původ a případný vlastní zobrazovaný název |
| Typ | Rezim, Specializace | Lek.JeLek | Samostatná větev Lék / Prostředek; lék není nový druh kontejneru |
| Umístění | MistnostId, KontejnerId | LekarnickaId | Zachovat vazbu balení na sadu, povolit Lekarnicka i PrvniPomoc; místnost odvodit ze sady |
| Kategorie | KategorieId | Není | Sekce v UI + případné vlastní štítky; ATC je samostatný odborný údaj |
| EAN | Ean | Lek.Ean, katalog.Ean | Vazba k produktovému balení, uložená jako text; ručně potvrzená a auditovatelná |
| Síla, forma | Pouze obecné Model | Katalog.Sila, Forma; Lek nic | Strukturovaná pole přípravku; nevkládat do Model ani pouze slepeného názvu |
| Léčivé látky | Není | Není ani v katalogu | Více látek na přípravek, zachovat zdrojový text a jednotky |
| Registrační číslo | Není | Není | Text u přípravku, odlišný od kódu SÚKL |
| Kód SÚKL | Není | Katalog.KodSukl je int? | Nullable text u produktového balení, doplnit jen ze skutečného zdroje |
| ATC a cesta podání | Není | Jen katalog.AtcWho | Pole přípravku; cestu podání doplnit z importu |
| Velikost balení | Není | Katalog.Baleni jako text | Zdrojový text + známé množství a jednotka u produktu; neodhadovat |
| Aktuální zásoba | Mnozstvi decimal?, Jednotka | Stejná pole v Lek | Jediný zůstatek každého fyzického balení, NULL = neznámý, pohyby zvlášť |
| Expirace | Expirace, SledovatExpiraci | Lek.Expirace; vlastní přepínač nemá | U fyzického balení, přesnost a explicitní neznámý stav, sledování a upozornění |
| Otevření a šarže | SerioveCislo není šarže | Není | DatumOtevreni, termín po otevření se zdrojem, Sarze jako text |
| Použití | Poznamka | Lek.NaCoJe bez zdroje | Zdrojované shrnutí navázané na verzi PIL; vlastní poznámka zvlášť |
| Výdej na předpis | Není | Lek.NaPredpis bool; katalog.Vydej | Původní kód výdeje + odvozený stav ano/ne/nezjištěno; neznámé není false |
| Pro koho | Není | Lek.ProKoho text | Volitelná soukromá poznámka či budoucí vazba; nevyžadovat pro inventuru |
| Dávkování | Není | Lek.Davkovani text | Volitelná soukromá poznámka s původem; průvodce dávku nenavrhuje |
| Nežádoucí účinky, interakce | Není | Dvě textová pole Lek | Primárně odkaz na úplný PIL, existující poznámky zachovat označené jako uživatelské |
| PIL a jeho verze | Není | Není | Samostatná metadata a soubor, vazba na přípravek, datum/verze/otisk |
| Pořízení a cena | DatumPorizeni, CenaKc | Není | Volitelně u fyzického balení, mimo základní průvodce |
| Výrobce | Vyrobce | Není | Držitel registrace a výrobce jsou různé údaje; neslévat je |
| NFC | Kod, NfcUid, MaVlastniNfcKartu | Není | Standardně NFC sady; případná karta krabičky až přes samostatnou evidenci karet |
| Servis, SPZ, záruka, pojištění | Spz, DalsiServisDo, ZarukaMesice/Do, příznaky a kolekce | Není | U léku nezobrazovat ani nevyplňovat; záruka 24 měsíců není expirace |
| Stav a audit | Aktivni, VytvorenoUtc, UpravenoUtc | Jen Lek.VytvorenoUtc; katalog.NactenoUtc | Stav balení, vytvoření/změna, historie pohybů, aplikační verze pro souběh |
| Poznámka | Poznamka | Lek.Poznamka | Zachovat jako soukromou poznámku oddělenou od importovaných údajů |

Kolekce Obsah, Leky, Pojisteni a ServisniZaznamy jsou navigační vztahy EF, nikoli samostatné skalární SQL sloupce položky.

## 5. Navržené tabulky a vazby

Názvy jsou návrh; existující schéma se nemění bez migrace.

| Entita | Hlavní pole a vazby |
|---|---|
| LecivyPripravek | Id, Nazev, Sila, FormaKod/Text, CestaPodaniKod/Text, RegistracniCislo, Atc, DrzitelRegistrace, StavRegistrace, Puvod, ImportDavkaId?, StavDoplneni |
| LecivaLatkaPripravku | Id, PripravekId, název/kód látky podle zdroje, množství a jednotka jen pokud skutečně dostupné; 1:N |
| LekovyProdukt | Id, PripravekId, KodSukl?, BaleniText?, Velikost?, Jednotka?, VydejKod?; 1:N z přípravku |
| ProduktovyKod | Id, ProduktId, TypKodu, Hodnota text, Puvod, PotvrzenoUtc; více kódů produktu, konflikt aktivního kódu vyžaduje vyřešení |
| LekBaleni | Id, ProduktId, LekarnickaId, Zbyva?, Jednotka, Expirace?, PresnostExpirace, DatumOtevreni?, PouzitelnePoOtevreniDo?, ZdrojPravidla?, Sarze?, Stav, sledování, soukromé poznámky, audit, Verze |
| PohybLeku | Id, BaleniId, typ příjem/spotřeba/přesun/inventura/vyřazení, množství a jednotka podle operace, původní/cílová sada u přesunu, čas, OperationId |
| PilDokument | Id, zdrojový identifikátor, cesta, zdrojová URL pokud existuje, verze/datum pokud známé, importní datum, otisk |
| PripravekPil | PripravekId, PilDokumentId, stav ověření vazby; M:N dovolí společný dokument a historii verzí |
| PopisPouziti | PripravekId, PilDokumentId, stručný text, stav revize, datum schválení; uživatelská poznámka není tento popis |
| ImportDavka | Id, zdroj, typ datasetu, verze/datum exportu, importní čas, otisk, výsledek importu |

Katalog bez EAN je validní. Produkt bez známé velikosti lze uložit jako neúplný ruční záznam a později doplnit. Do přípravku nedávat zůstatek ani expiraci. Mazání sady nesmí kaskádově zničit evidenci a historii balení; vyžadovat přesun nebo archivaci.

## 6. Ukládání, import a migrace

- Průvodce používá vlastní vstupní model, nikoli přímé bindování databázových entit. Na závěrečném POST ověřit přihlášení, CSRF, cílovou sadu, produkt a přípustné hodnoty.
- Založení balení, potvrzení kódu a počáteční pohyb zapisovat společně v transakci. OperationId s unikátním omezením zajistí, že dvojklik či opakování po výpadku vrátí stejný výsledek.
- Opakovaný záběr skeneru pouze vyplní vstup. Nové balení vzniká až potvrzením. EAN neomezovat unikátně na tabulce fyzických balení.
- Změny zásob řešit atomicky, se záznamem pohybu a kontrolou aplikační verze; nepřepsat změnu z druhého telefonu. Záporný výsledek odmítnout, neskrývat chybný odběr oříznutím na nulu.
- Import doplnit o XLSX nebo jasně zdokumentovaný převod a mapování českých hlaviček. Podporovat oba listy dodaného exportu: metadata oddělit od dat přípravků. EAN není povinný.
- Přidat hledání podle názvu a filtr síly/formy. Lookup musí vrátit ID kandidátů, strukturovaná pole, zdroj a stav shody.
- Import nejprve připravit a validovat, teprve potom transakčně sloučit. Po zavedení cizích klíčů nesmí plné mazání katalogu rozbít vazby. Export a obnova musí zahrnout nové tabulky a vybrané PIL.
- Zavést migrace nad kopií existující databáze se zálohou a ověřenou obnovou. EnsureCreated nedoplní nové sloupce existujícím tabulkám.
- Staré řádky Lek převést konzervativně jako samostatné záznamy zásoby. Nevíme, zda dosavadní množství znamenalo jednu krabičku nebo několik balení; nejasné záznamy označit k ručnímu rozdělení. Neodhadovat expirace jednotlivých krabiček.
- Původní NaCoJe, Davkovani, Interakce a NezadouciUcinky zachovat jako uživatelské poznámky bez tvrzení o ověření SÚKL. Staré false u NaPredpis bez zdroje neposkytuje důkaz volného výdeje.
- Původní číselný KodSukl převést bez domýšlení ztracených úvodních nul; doplnit z autoritativního zdroje při spárování. Položky s JeLek=false převést do evidence prostředků se zachováním původního ID v migrační mapě.
- PIL z přibližně 3GB archivu připravit na PC podle roadmapy. Průvodce nepotřebuje celý archiv ani synchronní rozbalování při skenu.

## 7. Ověření hotové implementace

1. Známý EAN vybere potvrzený produkt a nevytvoří druhý katalogový záznam.
2. Neznámý EAN najde podle názvu obě varianty PARALEN 500MG; uživatel vědomě vybere tabletu nebo čípek.
3. Dodaný export bez EAN se načte, zachová registrační čísla a účinné látky a nevytvoří falešné kódy.
4. Dvě krabičky stejného produktu mají různé expirace a nezávislé zůstatky.
5. Neznámá expirace/množství zůstane viditelně neznámá; nevznikne nula ani dnešní datum.
6. Zpět, chyba validace a nedostupná kamera neztratí zadání. Zrušení průvodce nevytvoří zásobu.
7. Dvojklik, opakovaný POST a souběžná spotřeba ze dvou telefonů nevytvoří duplicitu ani nepřepíšou zůstatek.
8. Sada PrvniPomoc přijme lék stejným průvodcem; nepovolený nebo archivovaný rodič se odmítne i při ručně sestaveném POST.
9. Chybějící PIL neblokuje uložení. Leták jiné síly/formy se bez ověřené vazby nepřipojí.
10. Migrace a obnova zachovají původní množství, poznámky, umístění a existující NFC odkazy. Nejasné původní zásoby zůstanou označené k doplnění.

