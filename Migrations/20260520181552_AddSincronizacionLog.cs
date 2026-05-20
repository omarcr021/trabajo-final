using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace trabfinal.Migrations
{
    /// <inheritdoc />
    public partial class AddSincronizacionLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SincronizacionLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Entidad = table.Column<string>(type: "TEXT", nullable: false),
                    UltimaSincronizacion = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SincronizacionLogs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SincronizacionLogs");
        }
    }
}
