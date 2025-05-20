using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Columnsnameupdateandmoreloggingfields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PositionInQueue",
                table: "Lines",
                newName: "PositionInQueueWhenJoined");

            migrationBuilder.AddColumn<DateTime>(
                name: "DateLastUpdated",
                table: "PushNotifications",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateLastUpdated",
                table: "PushNotifications");

            migrationBuilder.RenameColumn(
                name: "PositionInQueueWhenJoined",
                table: "Lines",
                newName: "PositionInQueue");
        }
    }
}
