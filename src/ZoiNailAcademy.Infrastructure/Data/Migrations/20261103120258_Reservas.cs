using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZoiNailAcademy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Reservas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Reservas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Codigo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    EdicionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Metodo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Telefono = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Importe = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    StripeSessionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    StripePaymentIntentId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ExpiraEn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PagadaEn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CanceladaEn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExpiradaEn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RequiereRevision = table.Column<bool>(type: "boolean", nullable: false),
                    NotaInterna = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ConsentimientoEn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reservas_Ediciones_EdicionId",
                        column: x => x.EdicionId,
                        principalTable: "Ediciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StripeEventos",
                columns: table => new
                {
                    EventId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Tipo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ProcesadoEn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StripeEventos", x => x.EventId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_Codigo",
                table: "Reservas",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_EdicionId_Estado",
                table: "Reservas",
                columns: new[] { "EdicionId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_Email_Estado",
                table: "Reservas",
                columns: new[] { "Email", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_Estado_ExpiraEn",
                table: "Reservas",
                columns: new[] { "Estado", "ExpiraEn" });

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_StripeSessionId",
                table: "Reservas",
                column: "StripeSessionId",
                unique: true,
                filter: "\"StripeSessionId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_Telefono_Estado",
                table: "Reservas",
                columns: new[] { "Telefono", "Estado" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Reservas");

            migrationBuilder.DropTable(
                name: "StripeEventos");
        }
    }
}
