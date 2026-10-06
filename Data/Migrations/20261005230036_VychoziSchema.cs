using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NfcHomeManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class VychoziSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Kategorie",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nazev = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kategorie", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LekovyKatalog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Ean = table.Column<string>(type: "TEXT", nullable: false),
                    KodSukl = table.Column<int>(type: "INTEGER", nullable: true),
                    Nazev = table.Column<string>(type: "TEXT", nullable: false),
                    Sila = table.Column<string>(type: "TEXT", nullable: true),
                    Forma = table.Column<string>(type: "TEXT", nullable: true),
                    Baleni = table.Column<string>(type: "TEXT", nullable: true),
                    AtcWho = table.Column<string>(type: "TEXT", nullable: true),
                    Vydej = table.Column<string>(type: "TEXT", nullable: true),
                    NactenoUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LekovyKatalog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Mistnosti",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nazev = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mistnosti", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Polozky",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Kod = table.Column<string>(type: "TEXT", nullable: false),
                    NfcUid = table.Column<string>(type: "TEXT", nullable: true),
                    Nazev = table.Column<string>(type: "TEXT", nullable: false),
                    Rezim = table.Column<string>(type: "TEXT", nullable: false),
                    Specializace = table.Column<string>(type: "TEXT", nullable: false),
                    Spz = table.Column<string>(type: "TEXT", nullable: true),
                    KategorieId = table.Column<int>(type: "INTEGER", nullable: true),
                    MistnostId = table.Column<int>(type: "INTEGER", nullable: true),
                    KontejnerId = table.Column<int>(type: "INTEGER", nullable: true),
                    Vyrobce = table.Column<string>(type: "TEXT", nullable: true),
                    Model = table.Column<string>(type: "TEXT", nullable: true),
                    SerioveCislo = table.Column<string>(type: "TEXT", nullable: true),
                    Ean = table.Column<string>(type: "TEXT", nullable: true),
                    Mnozstvi = table.Column<decimal>(type: "decimal(18,3)", nullable: true),
                    Jednotka = table.Column<string>(type: "TEXT", nullable: true),
                    DatumPorizeni = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    CenaKc = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MaVlastniNfcKartu = table.Column<bool>(type: "INTEGER", nullable: false),
                    Verejna = table.Column<bool>(type: "INTEGER", nullable: false),
                    SledovatPojisteni = table.Column<bool>(type: "INTEGER", nullable: false),
                    SledovatExpiraci = table.Column<bool>(type: "INTEGER", nullable: false),
                    SledovatServis = table.Column<bool>(type: "INTEGER", nullable: false),
                    SledovatRevizi = table.Column<bool>(type: "INTEGER", nullable: false),
                    Expirace = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    ZarukaMesice = table.Column<int>(type: "INTEGER", nullable: false),
                    ZarukaDo = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    DalsiServisDo = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Aktivni = table.Column<bool>(type: "INTEGER", nullable: false),
                    Poznamka = table.Column<string>(type: "TEXT", nullable: true),
                    VytvorenoUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpravenoUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Polozky", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Polozky_Kategorie_KategorieId",
                        column: x => x.KategorieId,
                        principalTable: "Kategorie",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Polozky_Mistnosti_MistnostId",
                        column: x => x.MistnostId,
                        principalTable: "Mistnosti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Polozky_Polozky_KontejnerId",
                        column: x => x.KontejnerId,
                        principalTable: "Polozky",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Leky",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LekarnickaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nazev = table.Column<string>(type: "TEXT", nullable: false),
                    JeLek = table.Column<bool>(type: "INTEGER", nullable: false),
                    Ean = table.Column<string>(type: "TEXT", nullable: true),
                    Mnozstvi = table.Column<decimal>(type: "decimal(18,3)", nullable: true),
                    Jednotka = table.Column<string>(type: "TEXT", nullable: true),
                    NaCoJe = table.Column<string>(type: "TEXT", nullable: true),
                    ProKoho = table.Column<string>(type: "TEXT", nullable: true),
                    NaPredpis = table.Column<bool>(type: "INTEGER", nullable: false),
                    Davkovani = table.Column<string>(type: "TEXT", nullable: true),
                    NezadouciUcinky = table.Column<string>(type: "TEXT", nullable: true),
                    Interakce = table.Column<string>(type: "TEXT", nullable: true),
                    Expirace = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Poznamka = table.Column<string>(type: "TEXT", nullable: true),
                    VytvorenoUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leky", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Leky_Polozky_LekarnickaId",
                        column: x => x.LekarnickaId,
                        principalTable: "Polozky",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Pojisteni",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PolozkaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Pojistovna = table.Column<string>(type: "TEXT", nullable: false),
                    CisloSmlouvy = table.Column<string>(type: "TEXT", nullable: true),
                    Typ = table.Column<string>(type: "TEXT", nullable: true),
                    PlatnostOd = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    PlatnostDo = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    RocniCenaKc = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Poznamka = table.Column<string>(type: "TEXT", nullable: true),
                    VytvorenoUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pojisteni", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pojisteni_Polozky_PolozkaId",
                        column: x => x.PolozkaId,
                        principalTable: "Polozky",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServisniZaznamy",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PolozkaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Datum = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Typ = table.Column<string>(type: "TEXT", nullable: false),
                    Popis = table.Column<string>(type: "TEXT", nullable: false),
                    CenaKc = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Provozovna = table.Column<string>(type: "TEXT", nullable: true),
                    DalsiTerminDo = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    VytvorenoUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServisniZaznamy", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServisniZaznamy_Polozky_PolozkaId",
                        column: x => x.PolozkaId,
                        principalTable: "Polozky",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LekovyKatalog_Ean",
                table: "LekovyKatalog",
                column: "Ean");

            migrationBuilder.CreateIndex(
                name: "IX_Leky_LekarnickaId",
                table: "Leky",
                column: "LekarnickaId");

            migrationBuilder.CreateIndex(
                name: "IX_Pojisteni_PolozkaId",
                table: "Pojisteni",
                column: "PolozkaId");

            migrationBuilder.CreateIndex(
                name: "IX_Polozky_KategorieId",
                table: "Polozky",
                column: "KategorieId");

            migrationBuilder.CreateIndex(
                name: "IX_Polozky_Kod",
                table: "Polozky",
                column: "Kod",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Polozky_KontejnerId",
                table: "Polozky",
                column: "KontejnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Polozky_MistnostId",
                table: "Polozky",
                column: "MistnostId");

            migrationBuilder.CreateIndex(
                name: "IX_Polozky_NfcUid",
                table: "Polozky",
                column: "NfcUid",
                unique: true,
                filter: "NfcUid IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ServisniZaznamy_PolozkaId",
                table: "ServisniZaznamy",
                column: "PolozkaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LekovyKatalog");

            migrationBuilder.DropTable(
                name: "Leky");

            migrationBuilder.DropTable(
                name: "Pojisteni");

            migrationBuilder.DropTable(
                name: "ServisniZaznamy");

            migrationBuilder.DropTable(
                name: "Polozky");

            migrationBuilder.DropTable(
                name: "Kategorie");

            migrationBuilder.DropTable(
                name: "Mistnosti");
        }
    }
}
