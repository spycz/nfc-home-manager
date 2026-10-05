using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NfcHomeManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class OddeleneTerminy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DalsiTerminTyp",
                table: "ServisniZaznamy",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Terminy",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PolozkaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Typ = table.Column<string>(type: "TEXT", nullable: false),
                    DatumDo = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Poznamka = table.Column<string>(type: "TEXT", nullable: true),
                    VytvorenoUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpravenoUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Terminy", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Terminy_Polozky_PolozkaId",
                        column: x => x.PolozkaId,
                        principalTable: "Polozky",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Terminy_PolozkaId_Typ",
                table: "Terminy",
                columns: new[] { "PolozkaId", "Typ" },
                unique: true);

            // Prevest dosavadni spolecne pole "dalsi servis / STK" na termin
            // jednoho druhu. Puvodni druh se nikde neukladal, proto:
            // - sleduje-li se servis (nebo nic), je to servis;
            // - sleduje-li se jen revize/STK, je to STK u auta, jinak revize.
            // U polozky, ktera sleduje servis i STK zaroven, nelze druh poznat;
            // prevede se jako servis s poznamkou k overeni.
            // Sloupec Polozky.DalsiServisDo se maze az v dalsi migraci
            // (OdstranitSpolecnyTerminServisu), protoze prestavba tabulky v SQLite
            // nebezi v transakci - pri jejim selhani zustanou data zde prevedena.
            migrationBuilder.Sql("""
                INSERT INTO "Terminy" ("PolozkaId", "Typ", "DatumDo", "Poznamka", "VytvorenoUtc", "UpravenoUtc")
                SELECT "Id",
                       CASE
                           WHEN "SledovatServis" = 0 AND "SledovatRevizi" = 1 AND "Specializace" = 'Auto' THEN 'Stk'
                           WHEN "SledovatServis" = 0 AND "SledovatRevizi" = 1 THEN 'Revize'
                           ELSE 'Servis'
                       END,
                       "DalsiServisDo",
                       CASE
                           WHEN "SledovatServis" = 1 AND "SledovatRevizi" = 1
                               THEN 'Převedeno ze společného pole „servis / STK“ – ověř druh.'
                           ELSE NULL
                       END,
                       strftime('%Y-%m-%d %H:%M:%S', 'now'),
                       strftime('%Y-%m-%d %H:%M:%S', 'now')
                FROM "Polozky"
                WHERE "DalsiServisDo" IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Terminy");

            migrationBuilder.DropColumn(
                name: "DalsiTerminTyp",
                table: "ServisniZaznamy");
        }
    }
}
