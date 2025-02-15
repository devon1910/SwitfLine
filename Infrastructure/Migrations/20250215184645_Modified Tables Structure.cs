using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ModifiedTablesStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QueueItems_Events_QueueId",
                table: "QueueItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_QueueItems",
                table: "QueueItems");

            migrationBuilder.RenameTable(
                name: "QueueItems",
                newName: "QueueMembers");

            migrationBuilder.RenameColumn(
                name: "QueueId",
                table: "QueueMembers",
                newName: "EventId");

            migrationBuilder.RenameIndex(
                name: "IX_QueueItems_QueueId",
                table: "QueueMembers",
                newName: "IX_QueueMembers_EventId");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "QueueMembers",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddPrimaryKey(
                name: "PK_QueueMembers",
                table: "QueueMembers",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Queues",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QueueMemberId = table.Column<long>(type: "bigint", nullable: false),
                    IsAttendedTo = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Queues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Queues_QueueMembers_QueueMemberId",
                        column: x => x.QueueMemberId,
                        principalTable: "QueueMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Queues_QueueMemberId",
                table: "Queues",
                column: "QueueMemberId");

            migrationBuilder.AddForeignKey(
                name: "FK_QueueMembers_Events_EventId",
                table: "QueueMembers",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QueueMembers_Events_EventId",
                table: "QueueMembers");

            migrationBuilder.DropTable(
                name: "Queues");

            migrationBuilder.DropPrimaryKey(
                name: "PK_QueueMembers",
                table: "QueueMembers");

            migrationBuilder.RenameTable(
                name: "QueueMembers",
                newName: "QueueItems");

            migrationBuilder.RenameColumn(
                name: "EventId",
                table: "QueueItems",
                newName: "QueueId");

            migrationBuilder.RenameIndex(
                name: "IX_QueueMembers_EventId",
                table: "QueueItems",
                newName: "IX_QueueItems_QueueId");

            migrationBuilder.AlterColumn<long>(
                name: "UserId",
                table: "QueueItems",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_QueueItems",
                table: "QueueItems",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_QueueItems_Events_QueueId",
                table: "QueueItems",
                column: "QueueId",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
