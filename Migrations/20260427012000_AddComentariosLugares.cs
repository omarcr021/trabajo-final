using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace trabfinal.Migrations
{
    /// <inheritdoc />
    public partial class AddComentariosLugares : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ComentariosLugares",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LugarId = table.Column<int>(type: "INTEGER", nullable: false),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    Texto = table.Column<string>(type: "TEXT", nullable: false),
                    Calificacion = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaPublicacion = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComentariosLugares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComentariosLugares_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComentariosLugares_UsuarioId",
                table: "ComentariosLugares",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComentariosLugares");
        }
    }
}
