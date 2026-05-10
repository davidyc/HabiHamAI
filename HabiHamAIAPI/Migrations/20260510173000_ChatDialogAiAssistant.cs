using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HabiHamAIAPI.Migrations
{
    /// <inheritdoc />
    public partial class ChatDialogAiAssistant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ai_assistant_id",
                table: "chat_dialogs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_chat_dialogs_ai_assistant_id",
                table: "chat_dialogs",
                column: "ai_assistant_id");

            migrationBuilder.AddForeignKey(
                name: "FK_chat_dialogs_ai_assistants_ai_assistant_id",
                table: "chat_dialogs",
                column: "ai_assistant_id",
                principalTable: "ai_assistants",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_chat_dialogs_ai_assistants_ai_assistant_id",
                table: "chat_dialogs");

            migrationBuilder.DropIndex(
                name: "IX_chat_dialogs_ai_assistant_id",
                table: "chat_dialogs");

            migrationBuilder.DropColumn(
                name: "ai_assistant_id",
                table: "chat_dialogs");
        }
    }
}
