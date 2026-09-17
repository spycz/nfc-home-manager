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
- Import SÚKL skrýt mezi pokročilé nástroje, dokud nebude ověřen reálně použitelný zdroj s EAN. README už popisuje neúspěšnou zkušenost s exportem; import nemá být základ onboardingového toku.
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

## Doporučené pořadí realizace

| Etapa | Výsledek | Podmínka dokončení |
|---|---|---|
| E0 — ověřit směr | Vybrané dvě pilotní věci; krátký test NDEF zápisu a čtení na používaných telefonech | Jasně víme, kde lze zapisovat a jak se offline obsah čte |
| E1 — spolehlivý základ | Soukromí, validace, oddělené termíny, migrace a obnova | Test obnovy a přístupu projde; existující NFC odkazy fungují |
| E2 — každodenní použití | Průvodce, evidence karet, zápis s ověřením, rychlé akce a základní úspory provozu | Založení běžné věci do minuty; opakovaná operace nemění data dvakrát |
| E3 — přidaná hodnota přepisu | Offline souhrn a aktualizace verze na dvou pilotních kartách | Ověřeno bez internetu i při přerušeném zápisu; uživatel pozná zastaralá data |
| E4 — rozšíření podle používání | Vybrané nice-to-have funkce, případně nativní nástroj | Přínos potvrzen několika týdny skutečného používání |

**První implementační balík po E0:** bezpečný přístup k položkám + ověřená obnova dat + návrh modelu karet a termínů. Na něj naváže průvodce pro auto a lékárničku; teprve potom rozšířit profily a offline zápis.
