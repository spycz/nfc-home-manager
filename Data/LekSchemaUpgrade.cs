using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace NfcHomeManager.Data;

// Malý verzovaný aditivní upgrade pro existující databáze vytvořené EnsureCreated.
// Nevydává se za EF migraci. Před první změnou starého schématu vytváří SQLite backup.
public static class LekSchemaUpgrade
{
    public static void Apply(AppDbContext db)
    {
        var connection = (SqliteConnection)db.Database.GetDbConnection();
        var wasClosed = connection.State != System.Data.ConnectionState.Open;
        if (wasClosed) connection.Open();
        try
        {
            var columns = new HashSet<string>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "PRAGMA table_info('Leky')";
                using var reader = command.ExecuteReader();
                while (reader.Read()) columns.Add(reader.GetString(1));
            }
            if (!columns.Contains("LekarnickaId"))
                throw new InvalidOperationException("Neznámé schéma Leky. Upgrade vyžaduje původní databázi NFC Home Manager.");

            var additions = new Dictionary<string, string>
            {
                ["PripravekId"] = "INTEGER NULL REFERENCES LekPripravky(Id) ON DELETE RESTRICT",
                ["OperaceId"] = "TEXT NULL",
                ["Sarze"] = "TEXT NULL",
                ["DatumOtevreni"] = "TEXT NULL",
                ["SledovatExpiraci"] = "INTEGER NOT NULL DEFAULT 1",
                ["Verze"] = "INTEGER NOT NULL DEFAULT 0",
                ["UpravenoUtc"] = "TEXT NULL"
            };
            if (additions.Keys.All(columns.Contains)) return;

            // BackupDatabase zahrnuje i data ve WAL. Chyba zálohy zastaví upgrade.
            if (connection.DataSource != ":memory:" && !string.IsNullOrWhiteSpace(connection.DataSource))
            {
                var backupPath = Path.GetFullPath(connection.DataSource) +
                    $".before-lek-wizard-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}.db";
                using var backup = new SqliteConnection(new SqliteConnectionStringBuilder
                { DataSource = backupPath }.ToString());
                backup.Open();
                connection.BackupDatabase(backup);
            }

            using var transaction = connection.BeginTransaction(deferred: false);
            void Execute(string sql)
            {
                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = sql;
                command.ExecuteNonQuery();
            }
            Execute("""
                CREATE TABLE IF NOT EXISTS LekPripravky (
                    Id INTEGER NOT NULL CONSTRAINT PK_LekPripravky PRIMARY KEY AUTOINCREMENT,
                    Klic TEXT NOT NULL, Nazev TEXT NOT NULL, Sila TEXT NULL, Forma TEXT NULL,
                    Ean TEXT NULL, ObsahBaleni decimal(18,3) NULL, Jednotka TEXT NOT NULL,
                    VytvorenoUtc TEXT NOT NULL);
                CREATE UNIQUE INDEX IF NOT EXISTS IX_LekPripravky_Klic ON LekPripravky(Klic);
                CREATE UNIQUE INDEX IF NOT EXISTS IX_LekPripravky_Ean ON LekPripravky(Ean);
                """);
            // Opět načíst schéma až pod write lockem (druhý start procesu mohl čekat).
            columns.Clear();
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = "PRAGMA table_info('Leky')";
                using var reader = command.ExecuteReader();
                while (reader.Read()) columns.Add(reader.GetString(1));
            }
            foreach (var (name, definition) in additions)
                if (!columns.Contains(name)) Execute($"ALTER TABLE Leky ADD COLUMN {name} {definition}");
            Execute("CREATE UNIQUE INDEX IF NOT EXISTS IX_Leky_OperaceId ON Leky(OperaceId)");
            Execute("CREATE INDEX IF NOT EXISTS IX_Leky_PripravekId ON Leky(PripravekId)");
            transaction.Commit();
        }
        finally { if (wasClosed) connection.Close(); }
    }
}
