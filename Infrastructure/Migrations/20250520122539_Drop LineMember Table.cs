using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropLineMemberTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Lines_LineMembers_LineMemberId",
                table: "Lines");

            migrationBuilder.DropTable(
                name: "LineMembers");

            migrationBuilder.RenameColumn(
                name: "LineMemberId",
                table: "Lines",
                newName: "EventId");

            migrationBuilder.RenameIndex(
                name: "IX_Lines_LineMemberId",
                table: "Lines",
                newName: "IX_Lines_EventId");

            migrationBuilder.AlterColumn<double>(
                name: "TimeWaited",
                table: "Lines",
                type: "double precision",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "AvgServiceTimeWhenJoined",
                table: "Lines",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DayOfWeek",
                table: "Lines",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "NumActiveServersWhenJoined",
                table: "Lines",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PositionInQueue",
                table: "Lines",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TimeOfDay",
                table: "Lines",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TotalPeopleInQueueWhenJoined",
                table: "Lines",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Lines",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Lines_UserId",
                table: "Lines",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Lines_AspNetUsers_UserId",
                table: "Lines",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Lines_Events_EventId",
                table: "Lines",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Lines_AspNetUsers_UserId",
                table: "Lines");

            migrationBuilder.DropForeignKey(
                name: "FK_Lines_Events_EventId",
                table: "Lines");

            migrationBuilder.DropIndex(
                name: "IX_Lines_UserId",
                table: "Lines");

            migrationBuilder.DropColumn(
                name: "AvgServiceTimeWhenJoined",
                table: "Lines");

            migrationBuilder.DropColumn(
                name: "DayOfWeek",
                table: "Lines");

            migrationBuilder.DropColumn(
                name: "NumActiveServersWhenJoined",
                table: "Lines");

            migrationBuilder.DropColumn(
                name: "PositionInQueue",
                table: "Lines");

            migrationBuilder.DropColumn(
                name: "TimeOfDay",
                table: "Lines");

            migrationBuilder.DropColumn(
                name: "TotalPeopleInQueueWhenJoined",
                table: "Lines");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Lines");

            migrationBuilder.RenameColumn(
                name: "EventId",
                table: "Lines",
                newName: "LineMemberId");

            migrationBuilder.RenameIndex(
                name: "IX_Lines_EventId",
                table: "Lines",
                newName: "IX_Lines_LineMemberId");

            migrationBuilder.AlterColumn<int>(
                name: "TimeWaited",
                table: "Lines",
                type: "integer",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double precision");

            migrationBuilder.CreateTable(
                name: "LineMembers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EventId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    BasePriority = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LineMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LineMembers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LineMembers_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LineMembers_EventId",
                table: "LineMembers",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_LineMembers_UserId",
                table: "LineMembers",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Lines_LineMembers_LineMemberId",
                table: "Lines",
                column: "LineMemberId",
                principalTable: "LineMembers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
