using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HabiHamAIAPI.Migrations
{
    /// <inheritdoc />
    public partial class AiAssistantExtraFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ai_assistant_field_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ai_assistant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    field_key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    field_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_required = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_assistant_field_definitions", x => x.id);
                    table.ForeignKey(
                        name: "FK_ai_assistant_field_definitions_ai_assistants_ai_assistant_id",
                        column: x => x.ai_assistant_id,
                        principalTable: "ai_assistants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_ai_assistant_extras",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ai_assistant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    values_json = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_ai_assistant_extras", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_ai_assistant_extras_ai_assistants_ai_assistant_id",
                        column: x => x.ai_assistant_id,
                        principalTable: "ai_assistants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_ai_assistant_extras_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ai_assistant_field_definitions_ai_assistant_id_field_key",
                table: "ai_assistant_field_definitions",
                columns: new[] { "ai_assistant_id", "field_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ai_assistant_field_definitions_ai_assistant_id_sort_order",
                table: "ai_assistant_field_definitions",
                columns: new[] { "ai_assistant_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "IX_user_ai_assistant_extras_ai_assistant_id",
                table: "user_ai_assistant_extras",
                column: "ai_assistant_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_ai_assistant_extras_user_id_ai_assistant_id",
                table: "user_ai_assistant_extras",
                columns: new[] { "user_id", "ai_assistant_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_assistant_field_definitions");

            migrationBuilder.DropTable(
                name: "user_ai_assistant_extras");
        }
    }
}
