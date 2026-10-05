using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NfcHomeManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class OdstranitSpolecnyTerminServisu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DalsiServisDo",
                table: "Polozky");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "DalsiServisDo",
                table: "Polozky",
                type: "TEXT",
                nullable: true);

            // Zpet do jednoho pole: nejblizsi z terminu polozky.
            migrationBuilder.Sql("""
                UPDATE "Polozky"
                SET "DalsiServisDo" = (SELECT MIN("DatumDo") FROM "Terminy" WHERE "Terminy"."PolozkaId" = "Polozky"."Id");
                """);
        }
    }
}
