using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedFKcolumnnameagain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Events_AspNetUsers_CreatedByUserId",
                table: "Events");

            migrationBuilder.RenameColumn(
                name: "CreatedByUserId",
                table: "Events",
                newName: "CreatedBy");

            migrationBuilder.RenameIndex(
                name: "IX_Events_CreatedByUserId",
                table: "Events",
                newName: "IX_Events_CreatedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_Events_AspNetUsers_CreatedBy",
                table: "Events",
                column: "CreatedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Events_AspNetUsers_CreatedBy",
                table: "Events");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "Events",
                newName: "CreatedByUserId");

            migrationBuilder.RenameIndex(
                name: "IX_Events_CreatedBy",
                table: "Events",
                newName: "IX_Events_CreatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Events_AspNetUsers_CreatedByUserId",
                table: "Events",
                column: "CreatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
