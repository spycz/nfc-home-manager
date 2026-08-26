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

Po přiložení telefonu k štítku se otevře veřejná stránka položky (bez
přihlášení) se stavem záruky, historií servisu apod. Adresu pro zápis
najdeš na detailu položky v administraci (`/Polozky/Detail?id=…`).

## Databáze

SQLite soubor (`nfc-home.db` lokálně). Schéma se při prvním startu
vytvoří automaticky (`EnsureCreated`) a naplní se výchozím seznamem
místností a kategorií — žádné migrace nejsou potřeba.

Evidované údaje k položce: kategorie, místnost, výrobce/typ, sériové
číslo, datum pořízení, cena, délka a konec záruky, příští plánovaný
servis, poznámka. K položce lze přidávat neomezeně záznamů **servisu/
oprav** (i STK) a **pojištění** (hodí se pro auto — pojišťovna, číslo
smlouvy, platnost, roční cena).

U každé položky se navíc zvlášť zaškrtává, co se u ní má sledovat: má
vlastní NFC kartu, pojištění, obecnou expiraci, servisní interval,
revizi/STK. Sekce v administraci i na veřejné stránce se zobrazují jen
podle toho, co je relevantní — lampa tak není zahlcená poli pro
pojištění.

### Co NFC karta reprezentuje (`Rezim`)

- **Předmět** — běžná evidovaná věc (výchozí).
- **Krabice / místnost** (`Kontejner`) — naskenování ukáže seznam věcí
  uvnitř. Obsahem může být předmět bez vlastní karty (jen položka v
  seznamu) i předmět s vlastní kartou a vlastní veřejnou stránkou
  (např. krabice s barvami obsahuje váleček a fólii bez karty, ale
  elektrická stříkací pistole svou vlastní kartu má).
- **Lékárnička** — drží seznam léků/prostředků (`Lek`): název, expirace,
  na co je, pro koho v rodině, je-li na předpis, dávkování, nežádoucí
  účinky, s čím se nesmí kombinovat, a příznak lék/prostředek (náplast
  není lék, ale patří tam taky).
- **První pomoc** — samostatný druh krabice, odděleně od domácí
  lékárničky, pro obsah spojený jen s první pomocí.

### Specializace předmětu

`Predmet` navíc může mít `Specializace = Auto` (pole SPZ) nebo
`PlynovyKotel` — mění to jen doporučené sledované vlastnosti a popisky,
STK/revize a servis se pořád evidují přes běžné servisní záznamy.

Přehledová stránka (`/`) ukazuje věci, kterým se blíží konec záruky,
naplánovaný servis/STK, konec pojištění nebo expirace (včetně expirace
jednotlivých léků v lékárničce) — výhled 60 dní.

## Přihlášení

Administrace (vše kromě veřejné stránky `/p/{kod}`) vyžaduje přihlášení
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
  `X-Content-Type-Options`, `Referrer-Policy`, `Permissions-Policy`,
  `Cross-Origin-Opener-Policy`.
- `noindex` meta tag na všech stránkách + `robots.txt` zakazující
  procházení — inventář domácnosti (a hlavně lékárnička) se nemá dostat
  do vyhledávačů.
- Veřejná stránka `/p/{kod}` je bez přihlášení pro běžné předměty a
  krabice — smysl NFC skenování (fyzická blízkost pár cm ke štítku je
  dost silná "důvěra"). **Lékárnička a první pomoc jsou výjimka:** ty
  nesou rodinná zdravotní data, takže `/p/{kod}` na ně vyžaduje
  přihlášení, pokud dané zařízení/prohlížeč ještě není přihlášené.
  Prakticky to znamená: na svém telefonu se přihlásíš jednou (30denní
  cookie se sama prodlužuje), pak skenuješ bez dalšího otravování; z
  cizího PC nebo prohlížeče, který se nikdy nepřihlásil, tě to pošle na
  login. Implementováno v `Pages/P/Index.cshtml.cs`.

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
5. Ověř, že aplikační pool subdomény běží na **.NET 9** (stejně jako
   hlavní web) a že má právo zapisovat do své složky — SQLite soubor
   `nfc-home.db` se vytváří přímo vedle `.dll` při prvním startu.

Pokud Forpsi neumožní přiřadit subdoméně vlastní .NET aplikační pool
odděleně od hlavního webu, je potřeba to vyřešit na úrovni hostingu
(další webhosting balíček nebo IIS aplikace pod subdoménou) — tahle
appka na to není nijak vázaná, jen potřebuje vlastní spuštěný proces.
