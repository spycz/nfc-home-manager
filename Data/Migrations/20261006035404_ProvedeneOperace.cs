using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NfcHomeManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class ProvedeneOperace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProvedeneOperace",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Presmerovani = table.Column<string>(type: "TEXT", nullable: false),
                    VytvorenoUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProvedeneOperace", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProvedeneOperace_VytvorenoUtc",
                table: "ProvedeneOperace",
                column: "VytvorenoUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProvedeneOperace");
        }
    }
}
