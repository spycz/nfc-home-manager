using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NfcHomeManager.Data;
using NfcHomeManager.Models;
using NfcHomeManager.Pages.Leky;
using NfcHomeManager.Pages.Polozky;

var folder = Path.Combine(Path.GetTempPath(), "lek-wizard-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(folder);
var path = Path.Combine(folder, "test.db");
var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite($"Data Source={path};Foreign Keys=True").Options;
void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
PridatModel Page(AppDbContext db, LekWizardInput input)
{
    var page = new PridatModel(db) { Input = input, PageContext = new PageContext
    {
        HttpContext = new DefaultHttpContext(),
        ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
    }};
    var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
    System.ComponentModel.DataAnnotations.Validator.TryValidateObject(input,
        new System.ComponentModel.DataAnnotations.ValidationContext(input), results, true);
    foreach (var result in results) page.ModelState.AddModelError("Input", result.ErrorMessage ?? "Invalid");
    return page;
}
LekWizardInput Input(int sada) => new()
{
    LekarnickaId = sada, Nazev = "Test přípravek", Sila = "500 mg", Forma = "tableta",
    Jednotka = "tableta", ObsahBaleni = 20, Mnozstvi = 12,
    Expirace = new DateOnly(2030, 5, 31)
};
try
{
    int sada;
    using (var db = new AppDbContext(options))
    {
        DbInitializer.Initialize(db); // nová DB
        var p = new Polozka { Nazev = "Test sada", Kod = "test-sada", Rezim = NfcRezim.PrvniPomoc };
        db.Polozky.Add(p); db.SaveChanges(); sada = p.Id;
        // Reprodukce původního Lek schématu bez nových sloupců.
        db.Database.ExecuteSqlRaw("DROP TABLE Leky");
        db.Database.ExecuteSqlRaw("DROP TABLE LekPripravky");
        db.Database.ExecuteSqlRaw("""
            CREATE TABLE Leky (
                Id INTEGER PRIMARY KEY AUTOINCREMENT, LekarnickaId INTEGER NOT NULL REFERENCES Polozky(Id) ON DELETE CASCADE,
                Nazev TEXT NOT NULL, JeLek INTEGER NOT NULL, Ean TEXT NULL,
                Mnozstvi decimal(18,3) NULL, Jednotka TEXT NULL, NaCoJe TEXT NULL,
                ProKoho TEXT NULL, NaPredpis INTEGER NOT NULL, Davkovani TEXT NULL,
                NezadouciUcinky TEXT NULL, Interakce TEXT NULL, Expirace TEXT NULL,
                Poznamka TEXT NULL, VytvorenoUtc TEXT NOT NULL);
            """);
        db.Database.ExecuteSqlInterpolated($"INSERT INTO Leky (LekarnickaId,Nazev,JeLek,Mnozstvi,Jednotka,NaPredpis,Poznamka,VytvorenoUtc) VALUES ({sada},{"Původní lék"},1,7,'ks',0,{"Zachovat"},{DateTime.UtcNow})");
        LekSchemaUpgrade.Apply(db);
        LekSchemaUpgrade.Apply(db); // opakování nic nepřepisuje
        var legacy = db.Leky.AsNoTracking().Single();
        Check(legacy.PripravekId is null && legacy.Mnozstvi == 7 && legacy.Poznamka == "Zachovat", "Legacy data changed");
        Check(Directory.GetFiles(folder, "*.before-lek-wizard-*.db").Length == 1, "Backup missing or repeated");
        using var backup = new SqliteConnection($"Data Source={Directory.GetFiles(folder, "*.before-lek-wizard-*.db")[0]}");
        backup.Open();
        using var command = backup.CreateCommand(); command.CommandText = "SELECT Mnozstvi FROM Leky";
        Check(Convert.ToDecimal(command.ExecuteScalar()) == 7, "Backup unreadable");
    }
    var first = Input(sada);
    using (var db = new AppDbContext(options))
    {
        var page = Page(db, first);
        Check(await page.OnPostAsync(false, default) is RedirectToPageResult, "First add failed");
    }
    using (var db = new AppDbContext(options))
        Check(await Page(db, first).OnPostAsync(false, default) is RedirectToPageResult, "Retry failed");
    var second = Input(sada); second.Expirace = new DateOnly(2031, 6, 30); second.Mnozstvi = 20;
    using (var db = new AppDbContext(options))
    {
        Check(await Page(db, second).OnPostAsync(false, default) is RedirectToPageResult, "Second add failed");
        Check(await db.LekPripravky.CountAsync() == 1, "Duplicate product");
        Check(await db.Leky.CountAsync() == 3, "Duplicate or missing box");
        Check((await db.Leky.Where(l => l.PripravekId != null).Select(l => l.Expirace).Distinct().CountAsync()) == 2, "Expirations merged");
    }
    using (var db = new AppDbContext(options))
    {
        var invalid = Input(sada); invalid.Mnozstvi = 21;
        Check(await Page(db, invalid).OnPostAsync(false, default) is PageResult, "Invalid balance accepted");
        var badParent = Input(-1);
        Check(await Page(db, badParent).OnPostAsync(false, default) is PageResult, "Invalid parent accepted");
        Check(await db.Leky.CountAsync() == 3, "Failed request wrote data");
    }
    using (var db = new AppDbContext(options))
    {
        var unknown = Input(sada); unknown.Mnozstvi = null; unknown.Expirace = null;
        unknown.MnozstviNezname = true; unknown.ExpiraceNeznama = true;
        Check(await Page(db, unknown).OnPostAsync(false, default) is RedirectToPageResult, "Unknown data rejected");
        var box = await db.Leky.SingleAsync(l => l.OperaceId == unknown.OperaceId);
        Check(box.Mnozstvi is null && box.Expirace is null, "Unknown data invented");
    }
    int boxId;
    using (var db = new AppDbContext(options))
    {
        var box = await db.Leky.SingleAsync(l => l.OperaceId == first.OperaceId); boxId = box.Id;
        var detail = new DetailModel(db);
        Check(await detail.OnPostUpravitMnozstviLekuAsync(sada, box.Id, -1, box.Verze, default) is RedirectResult, "Decrement failed");
    }
    using (var db = new AppDbContext(options))
    {
        var detail = new DetailModel(db);
        Check(await detail.OnPostUpravitMnozstviLekuAsync(sada, boxId, -1, 1, default) is ConflictObjectResult, "Stale decrement accepted");
        var stale = Input(sada); stale.LekId = boxId; stale.Verze = 1;
        Check(await Page(db, stale).OnPostAsync(false, default) is PageResult, "Stale edit accepted");
        Check((await db.Leky.FindAsync(boxId))!.Mnozstvi == 11, "Lost update");
    }
    Check(PridatModel.ValidBarcode("4006381333931"), "Valid EAN rejected");
    Check(!PridatModel.ValidBarcode("4006381333932"), "Bad checksum accepted");
    Console.WriteLine("PASS: schema upgrade + backup, retry, product sharing, separate boxes, validation, unknown values, stale edit/decrement, EAN.");
}
finally
{
    SqliteConnection.ClearAllPools();
    Directory.Delete(folder, recursive: true);
}
