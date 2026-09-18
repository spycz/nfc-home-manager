# Roadmapa NFC domácnosti

Návrh k 16. 9. 2026 podle současného kódu. Jde o plán, nikoli seznam hotových funkcí. Kapitoly nejsou seřazené podle priority; doporučené pořadí realizace je na konci.

**Směr:** přiložím telefon → vidím správný kontext → jedním potvrzením vyřídím běžný úkon. Programovatelná karta navíc umožní přenášet malý aktuální obsah i bez serveru.

## 1. Zdokonalení současného základu

- Oddělit fyzickou **NFC kartu** od **evidované věci**. Jedna věc může mít více karet; kartu lze vyměnit, vyřadit nebo přiřadit jinam bez ztráty historie věci.
- Rozdělit společné pole „další servis / STK“ na samostatné termíny: servis, STK, revize, výměna filtru. Jeden záznam dnes může přepsat termín jiného typu.
- Připomínky musí respektovat zapnuté sledování, vyřešené události a archivaci. Dnes se v `ReminderService` vyhodnocují data bez ohledu na příznaky sledování.
- Doplnit úpravu existujícího léku, servisu a pojištění; nestačí přidání a smazání. U léků rozlišit přípravek a jednotlivá balení s vlastní expirací.
- Validovat vztahy a pravidla na serveru: nepovolit cyklus krabic, nepovolené přímé vložení balení do vozidla (sadu první pomoci lze k vozidlu přiřadit), neplatné enumy ani libovolnou změnu množství mimo povolené meze.
- Zobrazit jasný stav neznámého, archivovaného či zrušeného štítku. Neznámý kód vracet jako HTTP 404.

## 2. Zabezpečení

- **Znalost URL není důkaz přiložení telefonu.** Odkaz lze opsat, sdílet a znovu otevřít. Opravit tento předpoklad i v komentářích a README.
- Výchozí nové položky soukromé; veřejnost zapínat vědomě. Veřejná projekce má výslovně vyjmenovaná pole, bez smluv, rodinných údajů a interních poznámek. Prověřit také obsah krabic a odkazy na jejich rodiče.
- Zavést zrušitelné náhodné tokeny pro sdílení, oddělené od interního ID. Delší token snižuje hádání, nenahrazuje přihlášení. Zachovat řízený přechod starých NFC odkazů.
- Každá změna dat přes autentizovaný POST s ochranou CSRF; načtení akční URL pouze otevře potvrzení. Opakované odeslání stejné operace nesmí dvakrát odečíst zásobu.
- Pro citlivé stránky a odpovědi nastavit `Cache-Control: no-store`; tokeny a zdravotní údaje neukládat do běžných logů. Zvážit `Referrer-Policy: no-referrer` pro sdílené odkazy.
- Ponechat existující hashování hesla, rate limiting a CSP. Ověřit důvěryhodné proxy, ochranu exportů, limity importů a trvalé bezpečné uložení Data Protection klíčů.
- Aktualizovat runtime a závislosti na podporované verze; testovat hlavně anonymní přístup, vnořené kontejnery a neoprávněné změny.

## 3. Efektivita síťového provozu

- Změřit první načtení NFC stránky a opakované návštěvy na mobilu; sledovat přenesené bajty a dobu do použitelného obsahu.
- Verzionovat CSS/JS obsahem a dlouhodobě cachovat statické soubory. Knihovnu ZXing načítat až při spuštění skeneru.
- Kompresi textových odpovědí řešit na jednom vhodném místě; u odpovědí kombinujících tajné a uživatelské hodnoty nejprve posoudit bezpečnost.
- Akce množství mohou vracet malou odpověď a změnit jen příslušný řádek; zachovat funkční formulář bez JavaScriptu. Žádný pravidelný polling bez potřeby.
- Pro externí EAN lookup přidat omezenou cache výsledků i nenalezených kódů a sloučení souběžných dotazů. Vlastní historie má dál přednost.
- Seznamy stránkovat; detail načítat po potřebných sekcích. Optimalizace SQL šetří server, objem HTML/JSON šetří mobilní data — měřit obojí zvlášť.
- Případná PWA cachuje primárně statické rozhraní. Ukládání soukromých dat offline musí být vědomá volba s možností smazání na sdíleném telefonu.

## 4. UI/UX: průvodce novou věcí a kartou

Průvodce má nejvýše pět krátkých kroků, tlačítko Zpět, zachování rozepsaných hodnot a možnost nepovinné údaje přeskočit. Rozhodování nejprve pomocí jasných pravidel, bez potřeby AI.

1. **Co zakládáš?** Věc / auto / kotel / krabici / lékárničku / první pomoc / samostatnou akční kartu.
2. **Základ:** název a umístění; podle typu sken EAN nebo výběr existujícího vzoru.
3. **Co chceš sledovat?** Nabídnout předvolby z tabulky, další údaje skrýt pod „Pokročilé“.
4. **Obsah a přístup:** u krabice přidávání věcí, u lékárničky opakované přidávání balení; nastavení soukromí a náhled stránky po přiložení.
5. **Připojit kartu:** bez karty / existující karta / nová karta → připravit zápis → přiložit → přečíst zpět → potvrdit shodu. Bez podpory zápisu nabídnout postup přes NFC Tools a QR.

| Volba | Nabídnuté údaje a další větev | Hlavní akce po přiložení |
|---|---|---|
| Auto | SPZ, volitelně VIN a km; samostatné STK, servis, pojištění | Zapsat servis / stav km |
| Kotel | Model, umístění, servisní kontakt; servis a revize zvlášť | Zapsat kontrolu |
| Běžná věc | Model, pořízení, záruka; volitelně množství a expirace | Detail / změna množství |
| Krabice | Umístění → přidat existující věc nebo rychle založit novou | Najít / přidat / přesunout obsah |
| Lékárnička | Přípravek → balení → expirace, množství; osobní údaje volitelné | Spotřeba / doplnění / expirace |
| První pomoc | Seznam vybavení a kontrola úplnosti; léky jen podle potřeby | Kontrolní seznam a chybějící zásoby |
| Akční karta | Vybrat existující věc a povolenou akci | Předvyplněné potvrzení akce |

- Změna typu přepočítá nabídku polí; před zahozením rozepsaných údajů ukáže dopad. Stejná pravidla platí i na serveru.
- Mobilní detail: název, stav, nejbližší termín a jedna hlavní akce nahoře; historie až níže. Velké dotykové cíle, popisky, chybové souhrny a ovládání klávesnicí.
- Oddělit stav „věc uložena“ od „karta úspěšně zapsána“. Selhání NFC zápisu nesmí vytvářet další kopie věci.
- Praktický cíl: založit obyčejnou věc do minuty a běžný úkon vyřídit nejvýše dvěma klepnutími po otevření stránky; ověřit na skutečném telefonu.

## 5. Vize: skutečné využití NTAG215

### Co hardware umožňuje a co od něj nečekat

NTAG215 je pasivní NFC Forum Type 2 tag s 504 bajty uživatelské paměti; NDEF a jeho obálka část prostoru spotřebují. Umožňuje přepis, 7bajtové UID, 24bitové počítadlo čtení a zrcadlení UID/počítadla do obsahu. Má 32bitové heslo a nevratné zamykání zápisu. Neprovádí vlastní aplikační program ani síťové požadavky. Heslo není šifrování a UID, počítadlo ani výrobní podpis neověřují oprávnění uživatele nebo čerstvost webového požadavku. [NXP: datasheet NTAG213/215/216](https://www.nxp.com/docs/en/data-sheet/NTAG213_215_216.pdf)

Web NFC umožňuje čtení a zápis NDEF v podporovaném Chrome na Androidu, přes HTTPS, s povolením a aktivní stránkou. Neposkytuje obecné nízkoúrovňové příkazy pro nastavení hesla a zrcadlení. Detekovat dostupnost API i chyby hardwaru; pro ostatní prostředí nabídnout externí nástroj. [Chrome: Web NFC](https://developer.chrome.com/docs/capabilities/nfc)

Pro iPhone zachovat běžný URL záznam jako první záznam na kartě; čtení odkazu systémem a vlastní zpracování dat jsou různé scénáře. Vlastní offline formát plánovat s kompatibilní čtecí aplikací, ne jako automatickou funkci Safari. [Apple: background tag reading](https://developer.apple.com/documentation/corenfc/adding-support-for-background-tag-reading)

### Navržené scénáře

| Scénář | Přínos | Využívá přepisovatelnost? | Etapa |
|---|---|---|---|
| Zápis a ověření karty z průvodce | Méně ručního kopírování a chyb při přiřazení | Ano, při vytvoření a změně obsahu | E2 |
| Přenosný stručný obsah krabice | Název, několik položek a datum aktualizace čitelné bez serveru | Ano, aktualizace NDEF dat | E3 |
| Servisní souhrn na kartě | Poslední kontrola a další termín dostupné kompatibilní čtečce offline | Ano, po potvrzeném servisu | E3 |
| Recyklace karty pro jinou věc či jiný systém | Nový odkaz i lokální obsah bez výměny štítku | Ano; pouhé přesměrování na serveru přepis nepotřebuje | E2 |
| Akční karty „doplnit“, „zkontrolovat“, „přesunout“ | Rychlé otevření správného formuláře | Samotná akce ne; funguje i s pevným URL | E2 |
| UID a počítadlo zrcadlené v URL | Experiment s rozlišením karet a četností čtení | Programování konfigurace; data mění čip | E3, experiment |

**Pilot:** jedna krabice a jedna servisní karta. NDEF obsahuje jako první záznam stabilní HTTPS URL, další záznam malý verzovaný souhrn bez osobních údajů. Před zápisem spočítat skutečnou délku v bajtech včetně NDEF/TLV; dlouhý obsah odmítnout nebo zkrátit se souhlasem. Neplnit kartu celým inventářem.

**Aktualizace:** databáze je hlavní zdroj pravdy. Uložit novou verzi → označit kartu „čeká na přepsání“ → fyzicky přiložit → zapsat → přečíst a porovnat → označit verzi za ověřenou. Při přerušení nabídnout opravu; zápis karty a databáze netvoří jednu atomickou transakci. Offline souhrn vždy ukazuje datum/verzi a může být zastaralý.

**Zrcadlení a ochrana:** nejprve laboratorní ověření vhodným nástrojem či nativní utilitou. Počítadlo čtení není počet dokončených úkonů a zkopírovaná URL může být přehrána. Heslem případně chránit zápis a ponechat veřejné čtení odkazu; správu hesel řešit mimo kartu. Nevratný zámek standardně nenabízet, protože ruší budoucí přepisování. [NXP: konfigurace, ochrana a počítadlo](https://www.nxp.com/docs/en/data-sheet/NTAG213_215_216.pdf)

Ověřit na konkrétním Androidu i iPhonu, v režimu letadlo, s přerušeným zápisem, starou verzí, uzamčenou kartou a na zamýšleném fyzickém povrchu. Nativní doprovodnou aplikaci stavět až tehdy, když pilot prokáže užitek nad rámec URL.

## 6. Databáze a správa dat

- **Ponechat SQLite** pro jednu domácnost. Přechod na jinou databázi řešit až podle souběhu zápisů a provozních potřeb.
- Zavést EF Core migrace místo samotného `EnsureCreated`. Pro stávající databázi připravit převzetí schématu do migrací, vyzkoušet na kopii a před nasazením zálohovat; nespouštět slepě počáteční migraci nad existujícími tabulkami.
- Navrhnout `NfcKarta` (token, volitelné UID, stav, zamýšlená/ověřená verze obsahu), historii přiřazení, samostatné termíny a pohyby zásob. UID je pomocný identifikátor, nikoli přihlašovací údaj.
- Přidat databázová omezení a aplikační validace vztahů; atomické změny zásob a kontrolu souběžných úprav, aby dva telefony nepřepsaly změny druhého.
- Omezit velké dotazy s více kolekcemi `Include` v detailu. Použít projekce nebo rozdělené dotazy podle měření; indexovat časté filtry a EAN, ne automaticky každý sloupec.
- Doplnit **obnovu ze zálohy**: verzovaný formát, validace, náhled změn, transakce a zachování kódů NFC. Současný JSON export neobsahuje katalog léků ani provozní konfiguraci, takže není kompletní provozní záloha.
- Automatické konzistentní zálohy SQLite vhodným backup postupem, kopie mimo server a pravidelný test obnovy. Při WAL nekopírovat pouze živý `.db` soubor bez zohlednění necheckpointovaných dat.
- Ujasnit archivaci, mazání a historii. Přílohy ukládat mimo veřejný webroot, v databázi držet metadata; zahrnout je do obnovy a autorizace.

## 7. Recenze nápadu, slabiny a co zahodit

**Silná stránka:** fyzické umístění věci je přirozený vstup do její evidence. Největší hodnotu mají krabice s obsahem, opakované kontroly, zásoby a termíny — tam je přiložení telefonu užitečné pravidelně.

**Slabiny:** náklady na první zadání, zastarávání dat, různá podpora telefonů a závislost online stránky na serveru. Současná směs inventáře, vozidel a zdravotních údajů může přerůst v dlouhé formuláře. Více zápisů na kartu také znamená více fyzické práce při aktualizacích.

- Zahodit univerzální formulář se všemi poli; nahradit jej profily a průvodcem.
- Import souboru SÚKL připravit jako zdroj doplňujících údajů pro scénář sken EAN → název → potvrzení přípravku (kapitola 10). Správa importu zůstává v pokročilých nástrojích; běžný průvodce používá již importovaný katalog. Ověřit skutečná pole souboru; chybějící vazba EAN nesmí blokovat dohledání podle názvu ani ruční evidenci.
- Sjednotit společný mechanismus lékárničky a první pomoci; zachovat odlišné šablony a pravidla soukromí.
- Nestavět vlastní databázi lékových interakcí ani automatická doporučení dávkování. Ponechat uživatelské poznámky a jasný původ případně převzatých údajů.
- Nezavádět mikroservisy, povinnou mobilní aplikaci ani AI pro jednoduché větvení formuláře.
- Nezapisovat na kartu každou změnu zásob. Offline souhrn má smysl jen tam, kde jeho dostupnost převáží riziko zastarání.
- QR ponechat jako zálohu. Hodnotu projektu měřit ušetřenou prací, ne počtem funkcí čipu; URL je vhodné řešení pro velkou část věcí.

## 8. Must have / nice to have

| Must have | Nice to have |
|---|---|
| Průvodce podle typu a serverová pravidla | Kopírování položek a vlastní šablony |
| Soukromí, zrušení sdílení a bezpečné akce | Rodinné účty, role a dočasný přístup servisáka |
| Záloha s ověřenou obnovou a migrace | Fotky, účtenky a návody |
| Oddělené termíny, přehled prošlých a vyřešených | Kalendářový export a volitelné notifikace |
| Úpravy záznamů a spolehlivé pohyby množství | Minimální zásoby a nákupní seznam |
| Evidence karet, přiřazení a ověření zápisu | Hromadné programování a tisk štítků |
| Náhradní QR a srozumitelné chyby skenu | Zapůjčení věcí a inventura místnosti |
| Pilot programovatelné karty s konkrétním užitkem | Offline PWA, nativní utilita a domácí automatizace |

## 9. Členění domácnosti do pěti sekcí

Doplněno 17. 9. 2026. Jde o návrh cílového uspořádání aplikace, nikoli o již implementované funkce.

Pět hlavních sekcí má vlastní přehledy, pole a hlavní akce. Společně používají evidenci věcí, umístění, NFC karet, příloh, termínů a historie.

| Sekce | Podsekce a příklady | Sledované údaje | Hlavní akce po přiložení |
|---|---|---|---|
| Lékárnička | Běžné léky; první pomoc | Přípravky, jednotlivá balení, množství, expirace; u první pomoci také úplnost sady | Zapsat spotřebu, doplnit, zkontrolovat výbavu |
| Vozidla | Auto, motorka, přívěs, kolo; případně zahradní traktůrek | Km nebo motohodiny, servis, STK, pojištění, pneumatiky a dokumenty podle typu | Zapsat servis, stav km nebo závadu |
| Technická zařízení budovy | Vytápění, voda, elektroinstalace, větrání, zabezpečení | Samostatné servisy, revize, kontroly, filtry, kontakty a dokumentace | Zapsat údržbu, kontrolu nebo závadu |
| Domácí spotřebiče | Kuchyň, praní a úklid, ostatní spotřebiče | Model, výrobní číslo, pořízení, záruka, návody, čištění a spotřební díly | Zapsat čištění, výměnu filtru nebo opravu |
| Dílna | Nářadí a stroje; materiál a spotřební zásoby | Stav, servis, zapůjčení; množství, jednotky a volitelná minimální zásoba | Odebrat, doplnit, zapůjčit nebo zapsat údržbu |

### Lékárnička: běžné léky a první pomoc

- **Běžné léky:** přehled přípravků; pod každým přípravkem konkrétní balení s vlastní expirací, množstvím a umístěním. Uživatel volí evidenci celých balení nebo obsahu s jednoznačnou jednotkou.
- **První pomoc:** jednotlivé sady (domácí, do auta, na výlet) s požadovaným a skutečným obsahem. Kontrola ukazuje chybějící vybavení a expirace tam, kde se sledují.
- Sdílený katalog umožní zařadit stejný přípravek i do první pomoci. Konkrétní fyzické balení má jedno umístění a nezapočítává se dvakrát.
- Kontrolní seznam první pomoci slouží k evidenci uživatelem zvolené výbavy; sám nepotvrzuje zdravotní nebo právní vhodnost sady.

### Sekce, umístění a kontejnery

- Sekce určuje účel a nabídku funkcí; umístění říká, kde věc je; kontejner určuje, v čem je uložená.
- Vrtačka: sekce Dílna → umístění Garáž → kufr č. 2. Přesun kufru zachová vazbu jeho obsahu.
- Autolékárnička: sekce Lékárnička / První pomoc → sada přiřazená ke konkrétnímu vozidlu. Detail vozidla nabídne odkaz na sadu a její stav, bez duplikace zásob.
- Krabice, kufr a sada mohou mít vlastní NFC kartu a existovat v kterékoli sekci. Přiřazení sady k vozidlu je odlišný vztah od vložení balení do sady.
- Zařízení obsluhující budovu patří do technických zařízení (kotel, rekuperace); zařízení pro konkrétní domácí činnost mezi spotřebiče (pračka, kávovar). Zařazení lze změnit se zachováním historie.
- NFC karta je samostatná evidence přiřazená k věci nebo kontejneru; výměna karty nevytváří novou věc.
- Server ověřuje povolené vztahy a zabraňuje cyklům. Nezakazovat legitimní vazbu sady první pomoci na vozidlo.

### Průvodce a společný přehled

První krok průvodce z kapitoly 4 nabídne sekci a poté typ v rámci stejného kroku. Následují základní údaje, sledování, obsah a přístup, připojení karty. Zachovat nejvýše pět krátkých kroků. Krabice je dostupná napříč sekcemi; samostatná akční karta se zakládá k existujícímu cíli.

Typ předvybere vhodná pole a úkony. Například kolo nevyžaduje SPZ ani STK. Úvodní přehled napříč sekcemi zobrazí blížící se a prošlé termíny, chybějící zásoby a otevřené závady; nabídne filtr podle sekce a umístění.

### Návrhy praktických scénářů

| Oblast | Umístění NFC | Průběh | Výsledek |
|---|---|---|---|
| Běžné léky – doplnění a spotřeba | Skříňka nebo zásobník | Otevřít přehled → vybrat přípravek a konkrétní balení → zadat spotřebu a potvrdit. Při nákupu přidat nové balení s vlastní expirací. | Správný zůstatek a přehled expirací; později seznam k doplnění podle nastaveného minima. |
| První pomoc – kontrola před výletem | Pouzdro sady | Otevřít kontrolní seznam → porovnat skutečný obsah → opravit množství → potvrdit kontrolu. | Seznam chybějících či expirujících položek a datum poslední kontroly; neúplná sada zůstává označená. |
| Vozidla – výměna oleje | Servisní složka nebo vhodné místo ve vozidle | Otevřít vozidlo → Zapsat servis → datum, km, provedené práce a případně cena → nastavit další termín nebo kilometrovou hranici. | Historie servisu a další plán bez přepsání STK či pojištění. Kilometrové upozornění vychází z naposledy ručně zadaného stavu. |
| Technická zařízení budovy – servis kotle | Štítek u kotle | Zobrazit poslední servis a kontakt → po provedení zapsat servis → potvrdit další plánovaný termín; později přiložit protokol a nabídnout export do kalendáře. | Dohledatelná historie a samostatné termíny servisu a revize; interval nastavuje uživatel podle podkladů zařízení. |
| Domácí spotřebiče – údržba kávovaru | Štítek u kávovaru | Otevřít detail s návodem a poslední údržbou → vybrat čištění, odvápnění nebo výměnu filtru → potvrdit provedení. | Oddělená historie a další termín pro každý úkon podle nastavení uživatele; později odkaz na správný spotřební díl. |
| Dílna – spotřební zásoby | Krabička se šrouby nebo zásobník materiálu | Otevřít položku → Odebrat / Doplnit → zadat množství v nastavené jednotce → potvrdit. | Dohledatelný pohyb a zůstatek; později upozornění na minimum a seznam k nákupu. |
| Dílna – zapůjčení nářadí | Kufr s nářadím | Otevřít nářadí → Zapůjčit → komu a očekávané vrácení → při návratu potvrdit převzetí a stav. | Přehled zapůjčených věcí a historie vrácení; rozšíření pro E4. |

Všechny scénáře mění data až po přihlášení a potvrzení; samotné načtení NFC odkazu nic neodečítá ani nepotvrzuje provedení údržby. Potvrzení jedné operace se při opakovaném odeslání neuplatní dvakrát. Běžné akce aktualizují databázi bez nutnosti přepisovat NFC kartu.

**Návaznost na etapy:** E1 připraví společná data, termíny a validace vztahů. E2 zavede pět sekcí a postupně základní scénáře; piloty zůstanou omezené na vybrané věci. Přílohy, kalendářový export, minimální zásoby, nákupní seznam a zapůjčení zůstávají rozšířeními E4, pokud je skutečné používání neposune do vyšší priority. E3 doplní offline souhrn pouze na zvolené pilotní karty.

## 10. Pilot: rodinná lékárnička a první pomoc

Podrobný [průvodce přidáním léku a porovnání současného a navrženého datového modelu](docs/LEK_WIZARD.md) vychází z kontroly kódu dne 18. 9. 2026. Obsahuje pět kroků, mapování polí Polozka/Lek/LekovyKatalog, návrh vazeb, migraci a podmínky ověření.

Návrh doplněný 17. 9. 2026, zatím bez implementace. Obecná šablona domácí a výletní sady. Konkrétní léčiva se zadávají až podle skutečných krabiček; neodhadovat název, sílu, formu ani dávkování. Osobní zdravotní údaje nejsou součástí této dokumentace.

### Uspořádání a výchozí obsah

- **První pomoc – doma:** jedna sada s přihrádkami Rány, Obvazy a Pomůcky.
- **První pomoc – výlety a sport:** menší samostatná sada s vlastními zásobami a NFC kartou.
- **Běžné léky:** společný katalog přípravků a skutečných balení; přípravek může být fyzicky v sadě první pomoci a dostupný z obou přehledů, ale zásoba se započítává pouze jednou.
- NFC otevírá sadu; čárový kód na výrobku pomáhá identifikovat produkt. Každá krabička nepotřebuje vlastní NFC.

Navržená počáteční zásoba pro domácí sadu (uživatelsky upravitelná šablona, nikoli povinný standard):

| Skupina | Položka | Cílová zásoba |
|---|---|---|
| Rány | Náplasti s polštářkem různých velikostí | 30 ks |
| Rány | Sterilní čtverce ve dvou velikostech | 10 jednotlivě sterilně balených jednotek |
| Rány | Nepřilnavé sterilní krytí | 4 ks |
| Rány | Náplast v roli | 1 role |
| Rány | Přípravek určený na dezinfekci drobných ran, vybraný podle označení výrobku | 1 malé balení |
| Obvazy | Hotový sterilní obvaz s polštářkem | 2 střední + 2 velké |
| Obvazy | Fixační obinadlo | 3 ks |
| Obvazy | Elastické obinadlo ve dvou šířkách | 2 ks |
| Obvazy | Trojcípý šátek | 2 ks |
| Pomůcky | Nitrilové rukavice ve vhodných velikostech | 4 páry |
| Pomůcky | Nůžky s tupou špičkou | 1 ks |
| Pomůcky | Pinzeta a pomůcka na odstranění klíštěte | Po 1 ks |
| Pomůcky | Izotermická fólie | 2 ks |
| Pomůcky | Jednorázový chladicí sáček | 2 ks |
| Pomůcky | Resuscitační rouška s ventilem | 1 ks |
| Pomůcky | Tištěný stručný návod první pomoci | 1 ks |
| Volitelné přípravky | Pouze skutečně evidované přípravky; název, sílu a formu převzít z krabičky | Podle skutečné zásoby |

Výletní sada: 10 náplastí, 4 balené jednotky sterilních čtverců, 2 hotové obvazy, 1 fixační obinadlo, 2 páry rukavic, 1 fólie, malé nůžky, pomůcka na klíště a náplasti na puchýře. Přesun zásob mezi sadami se zaznamenává jako přesun, nikoli jako nové pořízení.

Podklad pro druhy základního vybavení: [St John Ambulance – obsah lékárničky](https://shop.sja.org.uk/pages/what-to-put-in-a-first-aid-kit). Počty a rozdělení jsou návrhem pro tento projekt. Aplikace neposuzuje vhodnost léčiva pro konkrétního člena rodiny a neodvozuje dávkování z přibližného věku.

### Hlavní scénář: skutečné krabičky → EAN → název → údaje SÚKL

Uživatel si připraví skutečná léčiva z domácnosti a postupně je zadává. Nejdříve evidujeme, co skutečně vlastní; katalog slouží k doplnění údajů.

1. Otevřít lékárničku a zvolit **Přidat léčivo / Skenovat další krabičku**.
2. Kamerou naskenovat EAN na krabičce; vždy umožnit ruční zadání kódu. Načtený kód ukázat pro kontrolu.
3. Zapsat **název přípravku podle krabičky**. U již známého kódu nabídnout uložený název k potvrzení. Pro rozlišení variant doplnit sílu, lékovou formu a velikost balení.
4. Vyhledat nejprve v již potvrzených vlastních vazbách a poté v katalogu vytvořeném z **importovaného souboru databáze SÚKL**. Přímé vyhledání přes EAN použít jen tehdy, pokud konkrétní export nebo ověřený převodník tuto vazbu obsahuje. Jinak hledat podle názvu a upřesňujících údajů.
5. Zobrazit návrh shody: název, sílu, formu, velikost balení a kód SÚKL, jsou-li ve zdroji dostupné. Při více výsledcích vyžadovat výběr; samotný podobný název nestačí k automatickému přiřazení.
6. Po potvrzení převzít dostupné údaje, zejména **účinnou látku či látky**, a nabídnout **stručně, na co přípravek je**, pouze s dohledatelným podkladem. Chybějící pole ponechat prázdné.
7. Doplnit údaje konkrétní fyzické krabičky: expiraci, počáteční či zbývající množství, jednotku, umístění a volitelně šarži. U otevřených přípravků také datum otevření a dobu použitelnosti podle příslušných pokynů.
8. Zobrazit souhrn → **Uložit a skenovat další**. Po uložení zachovat vybranou sadu, nikoli expiraci nebo množství předchozího balení.

**Příklad bez domýšlení léčiva:** uživatel načte kód skutečné krabičky, napíše název z obalu a vybere přesnou variantu z importu. Účinná látka se doplní ze zdroje až po potvrzení shody. Nová krabička stejného přípravku používá stejnou katalogovou variantu, ale má vlastní expiraci a zůstatek.

### Import souboru SÚKL a původ informací

Dne 17. 9. 2026 byl prohlédnut ukázkový XLSX export SÚKL se 12 záznamy řady PARALEN (seznam dle registračního čísla). Obsahuje název, sílu, lékovou formu, registrační číslo, cestu podání, léčivé látky, ATC skupinu, držitele registrace a stav registrace. Neobsahuje EAN, kód SÚKL konkrétního balení, velikost balení ani popis použití a odkazy na PIL. Zjištění platí pro tento vzorek, nikoli pro všechny datové sady SÚKL. Registrační číslo, kód SÚKL a EAN uchovávat odděleně. Pro tento export se po skenu EAN potvrdí shoda podle názvu, síly a formy a velikost balení se doplní z krabičky.

- Správce nahraje soubor; aplikace zobrazí rozpoznaný formát, dostupná pole, datum zdroje a náhled několika řádků. Před importem ověřit strukturu, velikost a platnost záznamů.
- Importovat do lokálního katalogu v SQLite. Při běžném skenu vyhledávat v katalogu, ne znovu procházet celý soubor.
- Uchovat název a verzi zdroje, datum importu a identifikátor záznamu. Kód SÚKL a EAN/GTIN vést jako rozdílné identifikátory a ukládat jako text, včetně případných úvodních nul.
- **EAN identifikuje produktovou variantu, nikoli jedinečnou fyzickou krabičku.** Expiraci konkrétního balení nepřebírat z produktového katalogu. Pokud skener zachytí jiný druh kódu, například 2D DataMatrix, rozlišit jej a nepovažovat celý řetězec za EAN; jeho zpracování řešit samostatně.
- Uživatelem potvrzené přiřazení EAN ke katalogové variantě uložit pro příští skeny. Konflikt nové shody se starou ukázat k vyřešení; nepřepisovat jej automaticky.
- Stručné pole **„Na co je“** převzít z vhodného pole zdroje, pokud skutečně existuje a obsah odpovídá konkrétnímu přípravku. Jinak umožnit ručně schválené shrnutí podle jeho příbalové informace s odkazem a datem; uživatelskou poznámku označit odděleně.
- Nevytvářet indikaci pouhým odhadem z názvu, účinné látky nebo ATC skupiny. Pokud chybí podklad, zobrazit „Popis použití není doplněn“.
- Aktualizace katalogu zobrazí relevantní změny k posouzení a zachová uživatelské poznámky, potvrzené vazby i historii balení. Opakovaný import stejného zdroje nesmí duplikovat katalog.
- Import je pomůcka: nedostupný či neúplný soubor neblokuje založení skutečné krabičky ručně. Obvazy a další materiál se evidují samostatně, bez povinné shody v katalogu léčiv.

### Příbalové informace PIL: příprava na počítači

Dohodnutý postup: uživatel později stáhne archiv PIL na svůj počítač. Podle jeho údaje má archiv přibližně 3 GB; obsah ani struktura zatím nebyly prozkoumány. SÚKL uvádí PIL mezi dostupnými zdroji na [portálu otevřených dat](https://opendata.sukl.cz/).

1. Na počítači prohlédnout strukturu archivu a případný index či vazební soubor (například CSV/XML, bude-li přítomen).
2. Pro návrh importu poskytnout seznam souborů nebo snímek struktury, případný index a jeden ukázkový leták, ideálně Paralen. Celý archiv není potřeba nahrávat do konverzace.
3. Ověřit skutečný identifikátor propojující dokument s přípravkem. Neodvozovat vazbu pouze z názvu; ověřit odpovídající sílu a formu i případný společný dokument pro více variant.
4. Na počítači vytvořit výběr letáků k evidovaným přípravkům a přehled jejich ověřených vazeb. Hromadné zpracování oddělit od běžného skenování krabiček.
5. Na hosting přenést pouze vybrané dokumenty a metadata. Celý přibližně 3GB archiv nepřidávat do aplikace, repozitáře ani hostované SQLite.
6. Pokud se později ověří možnost stahování jednotlivých oficiálních dokumentů, lze ji využít pro doplnění nebo aktualizaci. Do té doby počítat s výběrem z lokálního archivu.

| Součást | Uložení a účel |
|---|---|
| Katalog přípravků | SQLite; hledání názvu, síly, formy a účinných látek |
| Vybrané PIL | Samostatné soubory na hostingu podle pravidel pro přílohy |
| Metadata PIL | SQLite; identifikátor zdroje, vazby na přípravky, cesta k souboru, dostupná verze či datum dokumentu, datum importu a kontrolní otisk |
| Stručně „Na co je“ | Schválené shrnutí z odpovídající příbalové informace s odkazem na dokument a jeho verzi |

Stejný ověřený dokument sdílet mezi odpovídajícími variantami a fyzickými baleními; neukládat jej znovu pro každou krabičku. Verzi dokumentu a datum importu rozlišovat. Aktualizace má zachovat dohledatelný zdroj shrnutí a označit shrnutí ze staršího dokumentu k revizi.

Detail přípravku nabídne stručný popis a tlačítko **Otevřít příbalový leták**, včetně dostupného data/verze. Chybějící nebo nejednoznačný PIL neblokuje evidenci balení; aplikace zobrazí, že leták dosud není přiřazen. Text „Na co je“ nevytvářet odhadem z ATC nebo účinné látky.

**Další krok:** počkat na stažení archivu uživatelem a podle ukázky ověřit formát indexu, vazbu na katalog a způsob výběru jednotlivých dokumentů. Stažení ani import nejsou zatím provedené.

### Údaje a běžné ovládání

| Evidence | Údaje |
|---|---|
| Katalogová varianta léčiva | Název, síla, forma, velikost balení, kód SÚKL, potvrzené EAN/GTIN vazby, účinné látky, zdrojovaný popis použití |
| Fyzické balení | Vlastní interní ID, vazba na variantu, expirace, množství a jednotka, umístění, případně šarže a datum otevření |
| Zdravotnický materiál a pomůcky | Název, případný produktový kód, množství a jednotka, umístění; expirace a stav obalu tam, kde se uplatňují |
| Sada první pomoci | Název, umístění, NFC karta, cílový obsah, skutečné zásoby, poslední a další kontrola |
| Pohyb zásob | Spotřeba, doplnění, přesun či oprava; datum, množství, dotčené balení a poznámka |

Po přiložení NFC zobrazit chybějící položky, blížící se expirace a poslední kontrolu. Hlavní akce: **Použil jsem vybavení**, **Doplnit**, **Zkontrolovat sadu**. Spotřebu více položek potvrdit najednou; opakované odeslání nesmí odečíst zásobu podruhé.

Výchozí, upravitelné nastavení: kontrola sady každých 6 měsíců, upozornění 60 dní před expirací. U sterilního materiálu kontrolovat i neporušenost obalu. Kontrola nezmění neúplnou sadu automaticky na kompletní. Připomínky respektují vyřazení balení a vypnuté sledování.

**Pilotní ověření:** známý EAN, neznámý EAN s ručním názvem, více variant stejného názvu, chybějící účinná látka či popis, opakovaný sken a dvě krabičky stejného přípravku s různou expirací. Opakovaný záběr kamery nezaloží další balení; další fyzická krabička vzniká vědomou akcí uživatele. Ověřit také neúspěšný import, zachování ručních dat po aktualizaci a přesun mezi domácí a výletní sadou.

**Návaznost:** tento scénář upřesňuje průvodce lékárničkou v E2. Ukázkový export již byl prohlédnut; další krok je vyzkoušet několik reálných krabiček a po stažení archivu PIL ověřit jeho index a vazby. U dodaného exportu použít potvrzení shody podle názvu, síly a formy; přímá vazba EAN vyžaduje jiný ověřený zdroj. Ruční evidence musí fungovat v obou případech.

## Doporučené pořadí realizace

| Etapa | Výsledek | Podmínka dokončení |
|---|---|---|
| E0 — ověřit směr | Vybrané dvě pilotní věci; krátký test NDEF zápisu a čtení na používaných telefonech | Jasně víme, kde lze zapisovat a jak se offline obsah čte |
| E1 — spolehlivý základ | Soukromí, validace, oddělené termíny, migrace a obnova | Test obnovy a přístupu projde; existující NFC odkazy fungují |
| E2 — každodenní použití | Průvodce, evidence karet, zápis s ověřením, rychlé akce a základní úspory provozu | Založení běžné věci do minuty; opakovaná operace nemění data dvakrát |
| E3 — přidaná hodnota přepisu | Offline souhrn a aktualizace verze na dvou pilotních kartách | Ověřeno bez internetu i při přerušeném zápisu; uživatel pozná zastaralá data |
| E4 — rozšíření podle používání | Vybrané nice-to-have funkce, případně nativní nástroj | Přínos potvrzen několika týdny skutečného používání |

**První implementační balík po E0:** bezpečný přístup k položkám + ověřená obnova dat + návrh modelu karet a termínů. Na něj naváže průvodce pro auto a lékárničku; teprve potom rozšířit profily a offline zápis.
