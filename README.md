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

Výstup vlož do `appsettings.Development.json` / `appsettings.Production.json`
pod `AdminAuth:PasswordHash`.

## Lokální spuštění

```bash
dotnet run
```

Aplikace poběží na `https://localhost:<port>` (dle profilu). Bez
nastaveného `AdminAuth:PasswordHash` se nelze přihlásit — vygeneruj si
ho příkazem výše.

## Nasazení na Forpsi (subdoména nfc.scitani1921.cz)

Postup vychází z `DEPLOY_FORPSI_WINDOWS.md` hlavního projektu, jen pro
samostatnou subdoménu:

1. V administraci Forpsi hostingu založ subdoménu `nfc.scitani1921.cz`
   a zjisti její cílovou složku na FTP (obvykle samostatná `/nfc` nebo
   zcela nový web root — liší se dle tarifu, ověř v Forpsi panelu).
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
