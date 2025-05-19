using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedIndexinPushNotificationTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "subscrition",
                table: "PushNotifications",
                newName: "Subscrition");

            migrationBuilder.CreateIndex(
                name: "IX_PushNotifications_UserId",
                table: "PushNotifications",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PushNotifications_UserId",
                table: "PushNotifications");

            migrationBuilder.RenameColumn(
                name: "Subscrition",
                table: "PushNotifications",
                newName: "subscrition");
        }
    }
}
