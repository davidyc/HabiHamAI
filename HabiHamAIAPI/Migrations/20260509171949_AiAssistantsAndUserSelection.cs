using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HabiHamAIAPI.Migrations
{
    /// <inheritdoc />
    public partial class AiAssistantsAndUserSelection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "selected_ai_assistant_id",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ai_assistants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    system_prompt = table.Column<string>(type: "text", nullable: false),
                    settings_json = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_assistants", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_users_selected_ai_assistant_id",
                table: "users",
                column: "selected_ai_assistant_id");

            migrationBuilder.CreateIndex(
                name: "IX_ai_assistants_sort_order",
                table: "ai_assistants",
                column: "sort_order");

            migrationBuilder.AddForeignKey(
                name: "FK_users_ai_assistants_selected_ai_assistant_id",
                table: "users",
                column: "selected_ai_assistant_id",
                principalTable: "ai_assistants",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_users_ai_assistants_selected_ai_assistant_id",
                table: "users");

            migrationBuilder.DropTable(
                name: "ai_assistants");

            migrationBuilder.DropIndex(
                name: "IX_users_selected_ai_assistant_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "selected_ai_assistant_id",
                table: "users");
        }
    }
}
