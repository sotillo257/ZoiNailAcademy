using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZoiNailAcademy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ListaEspera : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ListaEspera",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CursoId = table.Column<Guid>(type: "uuid", nullable: false),
                    EdicionPreferidaId = table.Column<Guid>(type: "uuid", nullable: true),
                    FechaPreferida = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Telefono = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ConsentimientoEn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TextoConsentimiento = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Contactado = table.Column<bool>(type: "boolean", nullable: false),
                    ContactadoEn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Notas = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BajaToken = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    BajaEn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Busqueda = table.Column<string>(type: "character varying(342)", maxLength: 342, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListaEspera", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ListaEspera_Cursos_CursoId",
                        column: x => x.CursoId,
                        principalTable: "Cursos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ListaEspera_Ediciones_EdicionPreferidaId",
                        column: x => x.EdicionPreferidaId,
                        principalTable: "Ediciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ListaEspera_BajaToken",
                table: "ListaEspera",
                column: "BajaToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ListaEspera_Contactado_CreatedAt",
                table: "ListaEspera",
                columns: new[] { "Contactado", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ListaEspera_CursoId_Email",
                table: "ListaEspera",
                columns: new[] { "CursoId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ListaEspera_EdicionPreferidaId",
                table: "ListaEspera",
                column: "EdicionPreferidaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ListaEspera");
        }
    }
}
