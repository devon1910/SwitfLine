using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddednewcolumninEventsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AverageTimeToServe",
                table: "Events",
                newName: "AverageTimeToServeSeconds");

            migrationBuilder.AddColumn<int>(
                name: "AverageTimeToServeMinutes",
                table: "Events",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AverageTimeToServeMinutes",
                table: "Events");

            migrationBuilder.RenameColumn(
                name: "AverageTimeToServeSeconds",
                table: "Events",
                newName: "AverageTimeToServe");
        }
    }
}
