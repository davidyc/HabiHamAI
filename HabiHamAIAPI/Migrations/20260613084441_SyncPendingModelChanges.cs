using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HabiHamAIAPI.Migrations
{
    /// <inheritdoc />
    public partial class SyncPendingModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_weekly_training_reviews");

            migrationBuilder.AddColumn<string>(
                name: "selected_ai_model",
                table: "users",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "selected_ai_model",
                table: "users");

            migrationBuilder.CreateTable(
                name: "user_weekly_training_reviews",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ai_assistant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    data_fingerprint = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    period_from = table.Column<DateOnly>(type: "date", nullable: false),
                    period_to = table.Column<DateOnly>(type: "date", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_weekly_training_reviews", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_weekly_training_reviews_ai_assistants_ai_assistant_id",
                        column: x => x.ai_assistant_id,
                        principalTable: "ai_assistants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_weekly_training_reviews_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_weekly_training_reviews_ai_assistant_id",
                table: "user_weekly_training_reviews",
                column: "ai_assistant_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_weekly_training_reviews_user_id_period_from_period_to",
                table: "user_weekly_training_reviews",
                columns: new[] { "user_id", "period_from", "period_to" },
                unique: true);
        }
    }
}
