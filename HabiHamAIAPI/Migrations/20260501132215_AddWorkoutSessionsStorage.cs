using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HabiHamAIAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkoutSessionsStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "workout_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_code = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    day = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workout_sessions", x => x.id);
                    table.ForeignKey(
                        name: "FK_workout_sessions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workout_exercises",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    meta = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    order_no = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workout_exercises", x => x.id);
                    table.ForeignKey(
                        name: "FK_workout_exercises_workout_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "workout_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workout_sets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    exercise_id = table.Column<Guid>(type: "uuid", nullable: false),
                    weight = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    reps = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    rpe = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    order_no = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workout_sets", x => x.id);
                    table.ForeignKey(
                        name: "FK_workout_sets_workout_exercises_exercise_id",
                        column: x => x.exercise_id,
                        principalTable: "workout_exercises",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_workout_exercises_session_id_order_no",
                table: "workout_exercises",
                columns: new[] { "session_id", "order_no" });

            migrationBuilder.CreateIndex(
                name: "IX_workout_sessions_user_id_date",
                table: "workout_sessions",
                columns: new[] { "user_id", "date" });

            migrationBuilder.CreateIndex(
                name: "IX_workout_sessions_user_id_session_code",
                table: "workout_sessions",
                columns: new[] { "user_id", "session_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_workout_sets_exercise_id_order_no",
                table: "workout_sets",
                columns: new[] { "exercise_id", "order_no" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "workout_sets");

            migrationBuilder.DropTable(
                name: "workout_exercises");

            migrationBuilder.DropTable(
                name: "workout_sessions");
        }
    }
}
