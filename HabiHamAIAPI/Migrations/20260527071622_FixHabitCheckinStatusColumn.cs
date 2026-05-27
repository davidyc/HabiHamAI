using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HabiHamAIAPI.Migrations
{
    /// <inheritdoc />
    public partial class FixHabitCheckinStatusColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE user_habit_checkins
                ADD COLUMN IF NOT EXISTS status character varying(20) NOT NULL DEFAULT 'done';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE user_habit_checkins
                DROP COLUMN IF EXISTS status;
                """);
        }
    }
}
