using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZoiNailAcademy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReservaBusqueda : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Busqueda",
                table: "Reservas",
                type: "character varying(342)",
                maxLength: 342,
                nullable: false,
                defaultValue: "");

            // Existing bookings: the same text TextoBusqueda.Normalizar builds (lower case, no Spanish accents).
            migrationBuilder.Sql("""
                UPDATE "Reservas"
                SET "Busqueda" = lower(translate(
                    "Nombre" || ' ' || "Email" || ' ' || "Telefono",
                    'ÁÀÂÄÉÈÊËÍÌÎÏÓÒÔÖÚÙÛÜÑÇáàâäéèêëíìîïóòôöúùûüñç',
                    'AAAAEEEEIIIIOOOOUUUUNCaaaaeeeeiiiioooouuuunc'));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Busqueda",
                table: "Reservas");
        }
    }
}
