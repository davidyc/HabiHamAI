using System;
using HabiHamAIAPI.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HabiHamAIAPI.Migrations;

/// <inheritdoc />
[DbContext(typeof(AppDbContext))]
[Migration("20260526154500_AddUserHabitsAndTodos")]
public partial class AddUserHabitsAndTodos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_habits",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_habits", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_habits_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_todo_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: true),
                    done_date = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_todo_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_todo_items_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_habit_checkins",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    habit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_habit_checkins", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_habit_checkins_user_habits_habit_id",
                        column: x => x.habit_id,
                        principalTable: "user_habits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_habit_checkins_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_habit_checkins_habit_id_date",
                table: "user_habit_checkins",
                columns: new[] { "habit_id", "date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_habit_checkins_user_id_date",
                table: "user_habit_checkins",
                columns: new[] { "user_id", "date" });

            migrationBuilder.CreateIndex(
                name: "IX_user_habits_user_id_is_active",
                table: "user_habits",
                columns: new[] { "user_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "IX_user_habits_user_id_sort_order",
                table: "user_habits",
                columns: new[] { "user_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "IX_user_todo_items_user_id_done_date",
                table: "user_todo_items",
                columns: new[] { "user_id", "done_date" });

            migrationBuilder.CreateIndex(
                name: "IX_user_todo_items_user_id_due_date",
                table: "user_todo_items",
                columns: new[] { "user_id", "due_date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_habit_checkins");

            migrationBuilder.DropTable(
                name: "user_todo_items");

            migrationBuilder.DropTable(
                name: "user_habits");
        }
    }
