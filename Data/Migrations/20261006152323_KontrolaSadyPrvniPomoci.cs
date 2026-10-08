using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NfcHomeManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class KontrolaSadyPrvniPomoci : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CilovaZasoba",
                table: "Leky",
                type: "decimal(18,3)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "KontrolovatObal",
                table: "Leky",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Skupina",
                table: "Leky",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "KontrolySady",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PolozkaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Datum = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Chybi = table.Column<int>(type: "INTEGER", nullable: false),
                    Prosle = table.Column<int>(type: "INTEGER", nullable: false),
                    BrzyExpiruje = table.Column<int>(type: "INTEGER", nullable: false),
                    Souhrn = table.Column<string>(type: "TEXT", nullable: true),
                    Poznamka = table.Column<string>(type: "TEXT", nullable: true),
                    VytvorenoUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KontrolySady", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KontrolySady_Polozky_PolozkaId",
                        column: x => x.PolozkaId,
                        principalTable: "Polozky",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KontrolySady_PolozkaId",
                table: "KontrolySady",
                column: "PolozkaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KontrolySady");

            migrationBuilder.DropColumn(
                name: "CilovaZasoba",
                table: "Leky");

            migrationBuilder.DropColumn(
                name: "KontrolovatObal",
                table: "Leky");

            migrationBuilder.DropColumn(
                name: "Skupina",
                table: "Leky");
        }
    }
}
