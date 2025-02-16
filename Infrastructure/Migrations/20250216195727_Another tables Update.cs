using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AnothertablesUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DateAttendedTo",
                table: "Lines",
                newName: "DateStartedBeingAttendedTo");

            migrationBuilder.AddColumn<DateTime>(
                name: "DateCompletedBeingAttendedTo",
                table: "Lines",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsOngoing",
                table: "Events",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateCompletedBeingAttendedTo",
                table: "Lines");

            migrationBuilder.DropColumn(
                name: "IsOngoing",
                table: "Events");

            migrationBuilder.RenameColumn(
                name: "DateStartedBeingAttendedTo",
                table: "Lines",
                newName: "DateAttendedTo");
        }
    }
}
