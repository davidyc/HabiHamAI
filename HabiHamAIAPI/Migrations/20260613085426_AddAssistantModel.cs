using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HabiHamAIAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddAssistantModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "model",
                table: "ai_assistants",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "model",
                table: "ai_assistants");
        }
    }
}
