using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZoiNailAcademy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class EmailsSalientes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailsSalientes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Para = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ReplyTo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Asunto = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Html = table.Column<string>(type: "text", nullable: false),
                    Texto = table.Column<string>(type: "text", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReferenciaId = table.Column<Guid>(type: "uuid", nullable: true),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Intentos = table.Column<int>(type: "integer", nullable: false),
                    ProximoIntentoEn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UltimoError = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ProveedorId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    EnviadoEn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailsSalientes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailsSalientes_Estado_ProximoIntentoEn",
                table: "EmailsSalientes",
                columns: new[] { "Estado", "ProximoIntentoEn" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailsSalientes_Tipo_ReferenciaId",
                table: "EmailsSalientes",
                columns: new[] { "Tipo", "ReferenciaId" },
                unique: true,
                filter: "\"ReferenciaId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailsSalientes");
        }
    }
}
