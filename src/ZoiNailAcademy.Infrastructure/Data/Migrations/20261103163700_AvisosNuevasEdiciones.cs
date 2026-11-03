using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZoiNailAcademy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AvisosNuevasEdiciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BajaUrl",
                table: "EmailsSalientes",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AvisosEnviados",
                columns: table => new
                {
                    ListaEsperaId = table.Column<Guid>(type: "uuid", nullable: false),
                    EdicionId = table.Column<Guid>(type: "uuid", nullable: false),
                    EncoladoEn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvisosEnviados", x => new { x.ListaEsperaId, x.EdicionId });
                    table.ForeignKey(
                        name: "FK_AvisosEnviados_Ediciones_EdicionId",
                        column: x => x.EdicionId,
                        principalTable: "Ediciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AvisosEnviados_ListaEspera_ListaEsperaId",
                        column: x => x.ListaEsperaId,
                        principalTable: "ListaEspera",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AvisosPendientes",
                columns: table => new
                {
                    CursoId = table.Column<Guid>(type: "uuid", nullable: false),
                    PendienteDesde = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Publicaciones = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvisosPendientes", x => x.CursoId);
                    table.ForeignKey(
                        name: "FK_AvisosPendientes_Cursos_CursoId",
                        column: x => x.CursoId,
                        principalTable: "Cursos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AvisosEnviados_EdicionId",
                table: "AvisosEnviados",
                column: "EdicionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AvisosEnviados");

            migrationBuilder.DropTable(
                name: "AvisosPendientes");

            migrationBuilder.DropColumn(
                name: "BajaUrl",
                table: "EmailsSalientes");
        }
    }
}
