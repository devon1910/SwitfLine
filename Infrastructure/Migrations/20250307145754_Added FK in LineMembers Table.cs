using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedFKinLineMembersTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "LineMembers",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_LineMembers_UserId",
                table: "LineMembers",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_LineMembers_AspNetUsers_UserId",
                table: "LineMembers",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LineMembers_AspNetUsers_UserId",
                table: "LineMembers");

            migrationBuilder.DropIndex(
                name: "IX_LineMembers_UserId",
                table: "LineMembers");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "LineMembers",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
