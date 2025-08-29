using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NewEventscolumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "address",
                table: "Events",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "latitude",
                table: "Events",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "longitude",
                table: "Events",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "radiusInMeters",
                table: "Events",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "address",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "latitude",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "longitude",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "radiusInMeters",
                table: "Events");
        }
    }
}
