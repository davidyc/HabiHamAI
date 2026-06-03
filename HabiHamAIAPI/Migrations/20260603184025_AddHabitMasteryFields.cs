using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HabiHamAIAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddHabitMasteryFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "days_to_master",
                table: "user_habits",
                type: "integer",
                nullable: false,
                defaultValue: 21);

            migrationBuilder.AddColumn<bool>(
                name: "is_mastered",
                table: "user_habits",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "days_to_master",
                table: "user_habits");

            migrationBuilder.DropColumn(
                name: "is_mastered",
                table: "user_habits");
        }
    }
}
