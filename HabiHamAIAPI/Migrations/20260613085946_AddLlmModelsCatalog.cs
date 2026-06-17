using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HabiHamAIAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddLlmModelsCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "selected_ai_model",
                table: "users");

            migrationBuilder.CreateTable(
                name: "llm_models",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    is_default = table.Column<bool>(type: "bit", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    sort_order = table.Column<int>(type: "int", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_llm_models", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_llm_models_name",
                table: "llm_models",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_llm_models_sort_order",
                table: "llm_models",
                column: "sort_order");

            var seedAt = new DateTime(2026, 6, 13, 8, 59, 46, DateTimeKind.Utc);
            migrationBuilder.InsertData(
                table: "llm_models",
                columns: ["id", "name", "label", "is_default", "is_active", "sort_order", "created_at_utc"],
                values: new object[,]
                {
                    { new Guid("a1b2c3d4-0001-4000-8000-000000000001"), "gpt-4", "GPT-4", true, true, 0, seedAt },
                    { new Guid("a1b2c3d4-0002-4000-8000-000000000002"), "gpt-4o", "GPT-4o", false, true, 10, seedAt },
                    { new Guid("a1b2c3d4-0003-4000-8000-000000000003"), "gpt-4o-mini", "GPT-4o mini", false, true, 20, seedAt },
                    { new Guid("a1b2c3d4-0004-4000-8000-000000000004"), "gpt-4.1", "GPT-4.1", false, true, 30, seedAt },
                    { new Guid("a1b2c3d4-0005-4000-8000-000000000005"), "gpt-4.1-mini", "GPT-4.1 mini", false, true, 40, seedAt },
                    { new Guid("a1b2c3d4-0006-4000-8000-000000000006"), "o3-mini", "o3-mini", false, true, 50, seedAt }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "llm_models");

            migrationBuilder.AddColumn<string>(
                name: "selected_ai_model",
                table: "users",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);
        }
    }
}
