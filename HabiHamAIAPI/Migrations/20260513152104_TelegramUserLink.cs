using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HabiHamAIAPI.Migrations
{
    /// <inheritdoc />
    public partial class TelegramUserLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "telegram_chat_id",
                table: "users",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "telegram_link_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash_sha256_hex = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    expires_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    consumed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_telegram_link_tokens", x => x.id);
                    table.ForeignKey(
                        name: "FK_telegram_link_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_users_telegram_chat_id",
                table: "users",
                column: "telegram_chat_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_telegram_link_tokens_token_hash_sha256_hex",
                table: "telegram_link_tokens",
                column: "token_hash_sha256_hex",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_telegram_link_tokens_user_id_consumed_at_utc",
                table: "telegram_link_tokens",
                columns: new[] { "user_id", "consumed_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "telegram_link_tokens");

            migrationBuilder.DropIndex(
                name: "IX_users_telegram_chat_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "telegram_chat_id",
                table: "users");
        }
    }
}
