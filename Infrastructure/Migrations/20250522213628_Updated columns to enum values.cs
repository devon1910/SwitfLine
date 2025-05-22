using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Updatedcolumnstoenumvalues : Migration
    {
        // Pseudocode:
        // 1. Add a temporary integer column "TimeOfDayInt".
        // 2. Map string values in "TimeOfDay" to their corresponding enum int values and update "TimeOfDayInt".
        // 3. Drop the original "TimeOfDay" column.
        // 4. Rename "TimeOfDayInt" to "TimeOfDay".

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PositionInQueueWhenJoined",
                table: "Lines");

            migrationBuilder.DropColumn(
                name: "TotalPeopleInQueueWhenJoined",
                table: "Lines");

            // 1. Add temporary int column
            migrationBuilder.AddColumn<int>(
                name: "TimeOfDayInt",
                table: "Lines",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // 2. Map string values to int (replace with your actual enum mapping)
            migrationBuilder.Sql(@"
                UPDATE ""Lines"" SET ""TimeOfDayInt"" = 
                    CASE ""TimeOfDay""
                        WHEN 'Morning' THEN 0
                        WHEN 'Afternoon' THEN 1
                        WHEN 'Evening' THEN 2
                        ELSE 0
                    END
            ");

            // 3. Drop old column
            migrationBuilder.DropColumn(
                name: "TimeOfDay",
                table: "Lines");

            // 4. Rename new column
            migrationBuilder.RenameColumn(
                name: "TimeOfDayInt",
                table: "Lines",
                newName: "TimeOfDay");

            // Repeat similar steps for "DayOfWeek"
            migrationBuilder.AddColumn<int>(
                name: "DayOfWeekInt",
                table: "Lines",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(@"
                UPDATE ""Lines"" SET ""DayOfWeekInt"" = 
                    CASE ""DayOfWeek""
                        WHEN 'Sunday' THEN 0
                        WHEN 'Monday' THEN 1
                        WHEN 'Tuesday' THEN 2
                        WHEN 'Wednesday' THEN 3
                        WHEN 'Thursday' THEN 4
                        WHEN 'Friday' THEN 5
                        WHEN 'Saturday' THEN 6
                        ELSE 0
                    END
            ");

            migrationBuilder.DropColumn(
                name: "DayOfWeek",
                table: "Lines");

            migrationBuilder.RenameColumn(
                name: "DayOfWeekInt",
                table: "Lines",
                newName: "DayOfWeek");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TimeOfDay",
                table: "Lines",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "DayOfWeek",
                table: "Lines",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "PositionInQueueWhenJoined",
                table: "Lines",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalPeopleInQueueWhenJoined",
                table: "Lines",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
