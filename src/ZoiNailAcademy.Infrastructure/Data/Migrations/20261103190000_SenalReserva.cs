using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZoiNailAcademy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SenalReserva : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PrecioReserva",
                table: "Cursos",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PrecioCurso",
                table: "Reservas",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            // Existing courses keep charging the whole price; existing bookings paid the whole price.
            migrationBuilder.Sql("""UPDATE "Cursos" SET "PrecioReserva" = "Precio";""");
            migrationBuilder.Sql("""UPDATE "Reservas" SET "PrecioCurso" = "Importe";""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrecioReserva",
                table: "Cursos");

            migrationBuilder.DropColumn(
                name: "PrecioCurso",
                table: "Reservas");
        }
    }
}
