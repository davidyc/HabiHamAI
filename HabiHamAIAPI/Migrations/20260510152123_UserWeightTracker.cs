using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HabiHamAIAPI.Migrations
{
    /// <inheritdoc />
    public partial class UserWeightTracker : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_weight_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    weight_kg = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_weight_entries", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_weight_entries_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_weight_entries_user_id_date",
                table: "user_weight_entries",
                columns: new[] { "user_id", "date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_weight_entries_user_id_updated_at_utc",
                table: "user_weight_entries",
                columns: new[] { "user_id", "updated_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_weight_entries");
        }
    }
}
