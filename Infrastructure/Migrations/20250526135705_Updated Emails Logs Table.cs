using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedEmailsLogsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_EmailDeliveryJobs",
                table: "EmailDeliveryJobs");

            migrationBuilder.RenameTable(
                name: "EmailDeliveryJobs",
                newName: "EmailDeliveryRequests");

            migrationBuilder.AlterColumn<string>(
                name: "Message",
                table: "EmailDeliveryRequests",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddPrimaryKey(
                name: "PK_EmailDeliveryRequests",
                table: "EmailDeliveryRequests",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_EmailDeliveryRequests",
                table: "EmailDeliveryRequests");

            migrationBuilder.RenameTable(
                name: "EmailDeliveryRequests",
                newName: "EmailDeliveryJobs");

            migrationBuilder.AlterColumn<string>(
                name: "Message",
                table: "EmailDeliveryJobs",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_EmailDeliveryJobs",
                table: "EmailDeliveryJobs",
                column: "Id");
        }
    }
}
