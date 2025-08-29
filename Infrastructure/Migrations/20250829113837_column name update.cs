using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class columnnameupdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "radiusInMeters",
                table: "Events",
                newName: "RadiusInMeters");

            migrationBuilder.RenameColumn(
                name: "longitude",
                table: "Events",
                newName: "Longitude");

            migrationBuilder.RenameColumn(
                name: "latitude",
                table: "Events",
                newName: "Latitude");

            migrationBuilder.RenameColumn(
                name: "address",
                table: "Events",
                newName: "Address");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RadiusInMeters",
                table: "Events",
                newName: "radiusInMeters");

            migrationBuilder.RenameColumn(
                name: "Longitude",
                table: "Events",
                newName: "longitude");

            migrationBuilder.RenameColumn(
                name: "Latitude",
                table: "Events",
                newName: "latitude");

            migrationBuilder.RenameColumn(
                name: "Address",
                table: "Events",
                newName: "address");
        }
    }
}
