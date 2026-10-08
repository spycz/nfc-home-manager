# NFC domácnost (nfc.scitani1921.cz)

Samostatná ASP.NET Core (Razor Pages) aplikace pro evidenci věcí v domácnosti
pomocí NFC štítků NTAG215. Nesouvisí s hlavním projektem Sčítání 1921 —
sdílí jen stejný Windows webhosting (Forpsi).

## Princip

Každá evidovaná věc (spotřebič, nářadí, auto…) dostane vlastní záznam
v databázi a vlastní krátký kód. Na fyzický NTAG215 štítek se zapíše
(např. aplikací **NFC Tools**) URL adresa ve tvaru:

```
https://nfc.scitani1921.cz/p/AB12XZ7
```

Po přiložení telefonu k štítku se otevře stránka položky se stavem
záruky, historií servisu apod. Nové položky jsou soukromé — stránku
uvidí jen přihlášený. Bez přihlášení se zobrazí jen položky, u kterých
je výslovně zaškrtnutá **veřejná stránka**. Adresu pro zápis najdeš na
detailu položky v administraci (`/Polozky/Detail?id=…`).

## Databáze

SQLite soubor (`nfc-home.db` lokálně). Schéma spravují **EF Core
migrace** (`Data/Migrations`), které se spustí automaticky při startu
aplikace; nová databáze se naplní výchozím seznamem místností a kategorií.

- **Záloha před migrací:** čeká-li existující databázi jakákoli migrace,
  `DbInitializer` nejdřív vytvoří konzistentní kopii
  `nfc-home.db.pred-migraci-<čas UTC>.bak` (přes `VACUUM INTO`, takže
  zahrne i data z WAL). Starší zálohy časem ručně smaž.
- **Převzetí starší databáze:** databáze vytvořená dřívější verzí přes
  `EnsureCreated` nemá historii migrací. Při prvním startu se ověří, že
  obsahuje všechny tabulky a sloupce výchozího schématu, a zapíše se jako
  migrace `VychoziSchema`; další migrace pak proběhnou běžně. Pokud
  schéma nesouhlasí, aplikace se nespustí a databázi nezmění.
- **Nová migrace** (vývoj): `dotnet ef migrations add Nazev -o Data/Migrations`
  (nástroj `dotnet tool install --global dotnet-ef`). Změny dat piš do
  migrace jako SQL a smazání sloupce dej do samostatné migrace — přestavba
  tabulky v SQLite neběží v transakci.

Po aktualizaci jsou všechny dosavadní položky **soukromé**. Na svém
přihlášeném telefonu se nic nezmění; u věcí, které mají jít otevřít i
bez přihlášení, zapni v úpravě položky „veřejná stránka“.

Evidované údaje k položce: kategorie, místnost, výrobce/typ, sériové
číslo, datum pořízení, cena, délka a konec záruky, poznámka. K položce
lze přidávat neomezeně záznamů **servisu/oprav** a **pojištění** (hodí
se pro auto — pojišťovna, číslo smlouvy, platnost, roční cena).
Servisní záznam, pojištění i lék jde dodatečně **upravit** (tlačítko
„Upravit“ u řádku na detailu). Úprava servisního záznamu mění jen
historii; plánovaný termín položky se nastavuje zvlášť.

**Plánované termíny** se vedou zvlášť podle druhu — servis, STK, revize,
výměna filtru, kontrola; od každého druhu má položka nejvýš jeden. Zápis
servisu s příštím termínem posune jen termín zvoleného druhu, takže
servis auta nepřepíše STK. Termíny STK a revize hlídá příznak
„revize / STK“, ostatní příznak „servisní interval“.

Při upgradu se dřívější společné pole „příští servis / STK“ převedlo na
jeden termín: servis, pokud se sleduje servis; jinak STK (auto) nebo
revize. U položek, které sledují servis i STK zároveň, druh poznat nešlo
— termín je převedený jako servis s poznámkou „ověř druh“ a je potřeba
ho na detailu případně opravit.

U každé položky se navíc zvlášť zaškrtává, co se u ní má sledovat: má
vlastní NFC kartu, pojištění, obecnou expiraci, servisní interval,
revizi/STK. Sekce v administraci i na stránce /p/{kod} se zobrazují jen
podle toho, co je relevantní — lampa tak není zahlcená poli pro
pojištění.

### Záloha a obnova (`/Admin/Export`, `/Admin/Obnova`)

Export stáhne celý obsah databáze jako jeden JSON soubor (formát
verze 2, `Services/Zaloha.cs`): místnosti, kategorie, položky včetně
kódů NFC štítků, servisní záznamy, plánované termíny, pojištění, léky
a katalog léků SÚKL. Soubor obsahuje soukromá data — nepatří do
repozitáře ani na sdílené úložiště.

Obnova má dva kroky. Po nahrání se soubor jen zkontroluje (platný
JSON, podporovaná verze, jedinečná Id a kódy štítků, existující vazby,
žádný cyklus krabic) a ukáže se srovnání počtů „teď v databázi“ ×
„v záloze“. Databáze se změní až po zaškrtnutí potvrzení:

- obnova **nahradí celý obsah** databáze, data se neslučují;
- běží v jedné transakci — při chybě zůstane databáze v původním stavu;
- předtím se současný stav uloží vedle databáze do
  `nfc-home.db.pred-obnovou-<čas UTC>.bak`;
- Id i kódy štítků se zachovají, takže odkazy na fyzických štítcích
  fungují dál.

Starší exporty (bez čísla verze, se společným polem „servis / STK“) jde
obnovit také: pole se převede na plánované termíny stejným pravidlem
jako při upgradu databáze a katalog léků zůstane prázdný.

### Co NFC karta reprezentuje (`Rezim`)

- **Předmět** — běžná evidovaná věc (výchozí).
- **Krabice / místnost** (`Kontejner`) — naskenování ukáže seznam věcí
  uvnitř. Obsahem může být předmět bez vlastní karty (jen položka v
  seznamu) i předmět s vlastní kartou a vlastní stránkou
  (např. krabice s barvami obsahuje váleček a fólii bez karty, ale
  elektrická stříkací pistole svou vlastní kartu má).
- **Lékárnička** — drží seznam léků/prostředků (`Lek`): název, expirace,
  na co je, pro koho v rodině, je-li na předpis, dávkování, nežádoucí
  účinky, s čím se nesmí kombinovat, a příznak lék/prostředek (náplast
  není lék, ale patří tam taky).
- **První pomoc** — sada první pomoci, zvláštní druh oddělený od domácí
  lékárničky. Má vlastní vybavení s cílovou zásobou a průvodce kontrolou
  (viz níže); vedle toho může jako krabice obsahovat i jiné předměty a
  jde přiřadit k vozidlu.

### Sada první pomoci a průvodce kontrolou

Vybavení sady jsou záznamy `Lek` se třemi poli navíc: **cílová zásoba**
(kolik má v sadě být; prázdné = volitelná položka), **skupina** (Rány,
Obvazy, Pomůcky, Přípravky…) a příznak **sterilní – kontrolovat obal**.

- **Šablony** (`Services/SablonySady.cs`): prázdnou sadu jde na detailu
  naplnit šablonou „Doma – 2 dospělí a dítě 11–13 let“ nebo „Výlety a
  sport“. Šablona je upravitelný návrh, ne zdravotní ani právní
  standard. Přípravky jsou jen obecné kategorie bez cílové zásoby —
  název, sílu a formu doplň podle skutečné krabičky; aplikace neposuzuje
  vhodnost léku ani dávkování.
- **Průvodce kontrolou** (`/Polozky/Kontrola?id=…`, tlačítko
  „Zkontrolovat sadu“ na detailu i na stránce po přiložení telefonu):
  jeden krok na skupinu, u každé položky skutečný počet a nejbližší
  expirace, poslední krok potvrzení a datum další kontroly (výchozí za
  6 měsíců). Tlačítko „Vše v této skupině je v plném stavu“ doplní
  cílové počty. Bez JavaScriptu je průvodce jedna dlouhá stránka.
- **Stav sady** se vždy počítá ze zapsaných zásob (`Services/StavSady.cs`):
  chybí, co je pod cílovou zásobou nebo není spočítané; prošlé a brzy
  expirující (60 dní) se hlásí zvlášť. Potvrzení kontroly neúplnou sadu
  úplnou neudělá.
- Každá kontrola se uloží do historie (`KontrolySady`) se souhrnem
  nálezů a nastaví plánovaný termín druhu „Kontrola“, který se u sady
  první pomoci hlídá v přehledu vždy.

### Specializace předmětu

`Predmet` navíc může mít `Specializace = Auto` (pole SPZ) nebo
`PlynovyKotel` — mění to jen doporučené sledované vlastnosti a popisky,
STK/revize a servis se evidují přes servisní záznamy a plánované termíny.

Přehledová stránka (`/`) ukazuje věci, kterým se blíží konec záruky,
naplánovaný servis/STK, konec pojištění nebo expirace (včetně expirace
jednotlivých léků v lékárničce) — výhled 60 dní. Servis/STK, pojištění
a obecná expirace se hlásí jen tehdy, když má položka zapnuté jejich
sledování; vypnutý příznak staré datum z připomínek vyřadí.

## Přihlášení

Administrace (vše kromě stránky `/p/{kod}` u veřejných položek) vyžaduje přihlášení
jedním účtem nastaveným v `appsettings`. Heslo se ukládá jako PBKDF2
hash, nikdy v čitelné podobě. Vygenerování hashe:

```bash
dotnet run -- hash-password TvojeHeslo
```

Výstup vlož pod `AdminAuth:PasswordHash` do `appsettings.Development.json`
(lokálně) resp. `appsettings.Production.json` (na hostingu) — oba soubory
jsou gitignored, založ si je podle přiložených `.example` šablon.

### Zabezpečení

- Přihlašovací formulář má vlastní přísný limit (8 požadavků/min na IP),
  zbytek webu mírnější globální limit — obrana proti hádání hesla hrubou
  silou.
- Admin cookie je `HttpOnly`, `SameSite=Lax` a v produkci jen přes HTTPS
  (`Secure`), v Developmentu jde i po HTTP kvůli lokálnímu testování.
- Odhlášení jde přes standardní Razor Pages formulář (CSRF token), ne
  přes holý endpoint.
- Bezpečnostní hlavičky: CSP (žádné inline skripty — veškeré JS je v
  `wwwroot/js/site.js`, potvrzovací dialogy a kopírování jdou přes
  `data-confirm`/`data-copy-target` atributy), HSTS, `X-Frame-Options`,
  `X-Content-Type-Options`, `Referrer-Policy` (`no-referrer`), `Permissions-Policy`,
  `Cross-Origin-Opener-Policy`.
- `noindex` meta tag na všech stránkách + `robots.txt` zakazující
  procházení — inventář domácnosti (a hlavně lékárnička) se nemá dostat
  do vyhledávačů.
- **Znalost URL není důkaz přiložení telefonu.** Odkaz ze štítku lze
  opsat, sdílet nebo znovu otevřít z historie prohlížeče. Stránka
  `/p/{kod}` proto bez přihlášení ukáže jen položku označenou jako
  **veřejná** (příznak `Verejna`, výchozí vypnutý), a to bez poznámky a
  sériového čísla. Soukromý obsah veřejné krabice ani soukromý rodičovský
  kontejner se nepřihlášenému nevypisují.
- **Lékárnička a první pomoc nejsou veřejné nikdy** — nesou rodinná
  zdravotní data. Na svém telefonu se přihlásíš jednou (30denní cookie
  se sama prodlužuje), pak skenuješ bez dalšího otravování; z cizího
  prohlížeče tě soukromá stránka pošle na login.
- Neznámý kód i archivovaná položka vrací nepřihlášenému HTTP 404 se
  stejnou hláškou, takže z odpovědi nejde poznat, které kódy existují.
  Implementováno v `Pages/P/Index.cshtml.cs`.
- **Opakované odeslání nemění data podruhé.** Formuláře s nevratnou
  akcí (±1 množství, přidání servisu, pojištění, léku nebo obsahu,
  založení položky) nesou náhodné `operaceId`. Server ho uloží spolu se
  změnou v jedné transakci; dvojklik, F5, Zpět + odeslat nebo souběžný
  požadavek se stejným ID jen přesměruje tam, kam vedla původní operace
  (`Services/JednorazovaOperace.cs`). Záznamy se mažou po 30 dnech.
  Prohlížeč navíc po odeslání zablokuje tlačítka formuláře.
- Všechny odpovědi kromě statických souborů mají `Cache-Control: no-store`,
  aby soukromé stránky (hlavně lékárnička) nezůstaly v mezipaměti
  prohlížeče; `Referrer-Policy: no-referrer` nepouští kód položky dál.
- Vztahy se ověřují na serveru (`Services/PolozkaPravidla.cs`): žádný
  cyklus krabic, obsah jen do krabice / první pomoci, léky jen do
  lékárničky, platné hodnoty výčtů a změna množství jen o ±1 bez
  podtečení pod nulu. K **vozidlu** lze přiřadit jen sadu první pomoci
  (autolékárnička) — zobrazí se pak na detailu vozidla. Do archivované
  krabice nejde nic nového vložit a ve výběru se nenabízí; věc, která
  v ní už je, v ní zůstat smí.
- Nepřihlášený nevidí na stránce věci název nadřazené krabice, pokud je
  krabice soukromá nebo archivovaná.

### Množství

Položky i jednotlivé léky/prostředky mají volitelné `Množství` +
`Jednotka` (např. „20 ks“, „1.5 kg“). Na detailu i ve výpisu léků v
lékárničce je u nich rychlé tlačítko **−1 / +1** pro odškrtnutí
spotřeby bez otevírání celého editačního formuláře.

### Skenování čárových kódů

Formuláře pro novou/upravovanou položku a pro přidání léku mají pole
**EAN** s tlačítkem *Skenovat*, které otevře kameru (vyžaduje HTTPS
nebo `localhost`/`127.0.0.1`) a čárový kód přečte přímo v prohlížeči —
knihovna `@zxing/browser` (MIT) je vendorovaná lokálně v
`wwwroot/js/vendor/zxing-browser.min.js`, žádné CDN, kvůli přísné CSP.

Po rozpoznání kódu se zavolá vlastní endpoint `/api/barcode/{ean}`
(jen pro přihlášené), který hledá ve třech krocích:

1. **Vlastní historie** — už jsi tenhle EAN někdy sám zadal (typicky
   při opakovaném nákupu stejného léku nebo výrobku). Nejspolehlivější
   zdroj, protože je přesně z tvojí domácnosti a nezávisí na žádné
   externí databázi — stačí u léku/položky jednou ručně napsat název a
   zároveň mít vyplněný naskenovaný EAN, a příště se stejný kód najde
   okamžitě. V praxi tohle pokryje přesně scénář „vezmu lék, co si
   kupuju pravidelně, a naskenuju“ — jednorázová investice jednoho
   ručního zápisu.
2. **Lokální databáze SÚKL** (viz níže) — pro léky, které jsi ještě
   nikdy nezadával.
3. **Open Food Facts** (obecné produkty, zdarma, bez klíče) — spíš pro
   běžné domácí věci než léky.

Pokud žádný krok nic nenajde, potichu selže — EAN zůstane vyplněný,
název se dopíše ručně.

**Léky konkrétně:** Open Food Facts je zaměřená na potraviny a běžné
spotřební zboží, ne na léčiva, takže u konkrétních léků často nic
nenajde. Pro české léky měla sloužit přesnější cesta — lokální databáze
SÚKL:

### Databáze léků SÚKL (`/Admin/ImportLeku`)

SÚKL nemá živé API klíčované EAN kódem, jen periodické bulk exporty
(opendata.sukl.cz, „Databáze léčivých přípravků“ / DLP). Řešení: soubor
se stáhne mimo appku (má normální internetové připojení, na rozdíl od
vývojového sandboxu, kde jsem tohle stavěl) a ručně nahraje na
`/Admin/ImportLeku` jako CSV/TSV. Import:

- si poradí s tabulátorem i středníkem jako oddělovačem (`CsvHelper`
  s `DetectDelimiter`),
- zvládne UTF-8 i Windows-1250 (starší CZ vládní exporty), s
  automatickou detekcí podle toho, jestli se po UTF-8 dekódování
  objeví náhradní znaky,
- vezme jen řádky s vyplněným EAN (ostatní jsou k ničemu pro
  vyhledávání podle skenu) a při více EAN kódech v jednom poli je
  rozdělí do samostatných záznamů,
- při každém nahrání **celý předchozí obsah nahradí** — spusť znovu,
  kdykoli si stáhneš čerstvější export.

**Update po ověření na reálných datech:** sloupec `EAN` je v celém SÚKL
DLP exportu prázdný, ne jen u starších registrací, jak jsem původně
předpokládal — SÚKL evidenci vede přes vlastní „Kód SÚKL“, čárové kódy
GS1/EAN přiděluje jiná autorita (GS1 Czech Republic) a do DLP se zjevně
nedostávají. Import a `/api/barcode/{ean}` krok pro SÚKL zůstávají v
kódu (jsou otestované a neškodí), pro případ, že by se to změnilo nebo
se našel jiný export/dataset se stejným sloupcovým formátem, ale reálně
teď tenhle krok skoro vždy nic nenajde. Hlavní praktickou hodnotu proto
má bod 1 výše (vlastní historie) — pokud znáš jiný veřejný zdroj, který
skutečně mapuje EAN kódy na české léky, klidně pošli odkaz.

Poznámka ke kompatibilitě: `@zxing/browser` funguje na desktopu
(Chrome/Edge) i v mobilním Safari (iPhone) přes standardní
`getUserMedia` — na rozdíl od nativního prohlížečového
`BarcodeDetector` API, které Safari nepodporuje.

## Lokální spuštění

```bash
cp appsettings.json.example appsettings.json
cp appsettings.Development.json.example appsettings.Development.json
dotnet run -- hash-password TvojeHeslo   # vlož výstup do PasswordHash v obou souborech
dotnet run
```

Aplikace poběží na `https://localhost:<port>` (dle profilu). Bez
nastaveného `AdminAuth:PasswordHash` se nelze přihlásit.

## Nasazení na Forpsi (subdoména nfc.scitani1921.cz)

Postup vychází z `DEPLOY_FORPSI_WINDOWS.md` hlavního projektu, jen pro
samostatnou subdoménu:

1. V administraci Forpsi hostingu založ subdoménu `nfc.scitani1921.cz`.
   Cílová FTP složka: `/subdoms/nfc` (samostatný web root, mimo hlavní
   `/www` používaný projektem Scitani1921).
2. Vytvoř `appsettings.Production.json` podle `appsettings.Production.json.example`
   (vlastní `AdminAuth:PasswordHash`, `AllowedHosts`).
3. Publish:
   ```powershell
   dotnet publish NfcHomeManager.csproj /p:PublishProfile=ForpsiFolder
   ```
4. Nahraj obsah `publish/forpsi/` do cílové složky subdomény (FTP údaje
   viz `.env.forpsi`, šablona v `.env.forpsi.example`).
5. Ověř, že aplikační pool subdomény běží na **.NET 10** a že má právo
   zapisovat do své složky — SQLite soubor `nfc-home.db` se vytváří přímo vedle `.dll` při prvním startu.

Pokud Forpsi neumožní přiřadit subdoméně vlastní .NET aplikační pool
odděleně od hlavního webu, je potřeba to vyřešit na úrovni hostingu
(další webhosting balíček nebo IIS aplikace pod subdoménou) — tahle
appka na to není nijak vázaná, jen potřebuje vlastní spuštěný proces.

**Ověřeno v praxi:** sdílený app pool s hlavním webem Scitani1921
nefunguje v žádné kombinaci hosting modelů — ANCM nedovolí ani dvě
in-process appky v jednom poolu (`500.35`), ani mix in-process a
out-of-process v jednom poolu (`500.34`). Buď vlastní app pool od
Forpsi, nebo úplně samostatný hosting (viz níže).

## Nasazení na vlastní Linux server (Oracle Cloud Free Tier apod.)

Alternativa k Forpsi, když sdílený app pool nejde vyřešit — appka běží
nativně na Linuxu (ASP.NET Core je cross-platform), za reverzní proxy
Caddy, která se stará o automatické HTTPS. Doménu není potřeba stěhovat
celou, stačí přesměrovat DNS záznam `nfc.scitani1921.cz` na IP nového
serveru, zbytek `scitani1921.cz` zůstává na Forpsi beze změny.

1. Založ VM (Ubuntu). Na Oracle Cloud Free Tier nezapomeň kromě
   lokálního firewallu (`ufw`) povolit porty **80** a **443** i v
   **VCN → Security Lists → Ingress Rules** — to je nejčastější důvod,
   proč appka "není vidět" i když na serveru vypadá vše v pořádku.
2. Spusť jednorázovou přípravu serveru (nainstaluje ASP.NET Core 10
   runtime, Caddy, vytvoří systémový účet a složky):
   ```bash
   sudo bash deploy/setup-server.sh
   ```
3. Publish (framework-dependent, běží přes nainstalovaný runtime):
   ```bash
   dotnet publish NfcHomeManager.csproj -c Release -o publish/linux
   ```
4. Nahraj obsah `publish/linux/` do `/opt/nfc-home-manager` na serveru
   (scp/rsync), včetně vyplněného `appsettings.Production.json`
   (`AdminAuth:PasswordHash`, `AllowedHosts: "nfc.scitani1921.cz"`).
   Slož vlastnictví zpět na servisní účet:
   ```bash
   sudo chown -R nfchome:nfchome /opt/nfc-home-manager
   ```
5. Nainstaluj systemd službu a Caddy konfiguraci:
   ```bash
   sudo cp deploy/nfc-home-manager.service /etc/systemd/system/
   sudo cp deploy/Caddyfile /etc/caddy/Caddyfile
   sudo systemctl daemon-reload
   sudo systemctl enable --now nfc-home-manager
   sudo systemctl reload caddy
   ```
6. Na Forpsi DNS správci subdomény `nfc.scitani1921.cz` přepni `A`
   záznam na veřejnou IP nového serveru.

Appka běží za reverzní proxy na `127.0.0.1:5299` — proto `Program.cs`
obsahuje `UseForwardedHeaders`, aby správně poznala, že originální
požadavek přišel přes HTTPS (jinak by `UseHttpsRedirection` skončil
v nekonečné smyčce přesměrování) a aby limiter přihlášení počítal podle
skutečné IP klienta, ne podle IP samotné proxy.
