using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZoiNailAcademy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class PreciosPorEdicion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Precio",
                table: "Ediciones",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PrecioReserva",
                table: "Ediciones",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            // Existing editions keep the prices their course has today.
            migrationBuilder.Sql("""
                UPDATE "Ediciones" e
                SET "Precio" = c."Precio", "PrecioReserva" = c."PrecioReserva"
                FROM "Cursos" c
                WHERE c."Id" = e."CursoId";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Precio",
                table: "Ediciones");

            migrationBuilder.DropColumn(
                name: "PrecioReserva",
                table: "Ediciones");
        }
    }
}
