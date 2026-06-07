using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HabiHamAIAPI.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ai_assistants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    assistant_code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    system_prompt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    settings_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    sort_order = table.Column<int>(type: "int", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    is_system = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    created_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_assistants", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "app_permissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    label = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    category = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    sort_order = table.Column<int>(type: "int", nullable: false),
                    is_system = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app_permissions", x => x.Id);
                    table.UniqueConstraint("AK_app_permissions_code", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "app_roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    label = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    is_system = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    sort_order = table.Column<int>(type: "int", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app_roles", x => x.Id);
                    table.UniqueConstraint("AK_app_roles_name", x => x.name);
                });

            migrationBuilder.CreateTable(
                name: "user_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    sort_order = table.Column<int>(type: "int", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ai_assistant_field_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ai_assistant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    field_key = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    field_type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    sort_order = table.Column<int>(type: "int", nullable: false),
                    is_required = table.Column<bool>(type: "bit", nullable: false),
                    is_system = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    created_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false)
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
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    telegram_chat_id = table.Column<long>(type: "bigint", nullable: true),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BirthDate = table.Column<DateOnly>(type: "date", nullable: true),
                    HeightCm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    WeightKg = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    City = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    About = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AiSummary = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    selected_ai_assistant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_users_ai_assistants_selected_ai_assistant_id",
                        column: x => x.selected_ai_assistant_id,
                        principalTable: "ai_assistants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "app_role_permissions",
                columns: table => new
                {
                    role_name = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    permission_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app_role_permissions", x => new { x.role_name, x.permission_code });
                    table.ForeignKey(
                        name: "FK_app_role_permissions_app_permissions_permission_code",
                        column: x => x.permission_code,
                        principalTable: "app_permissions",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_app_role_permissions_app_roles_role_name",
                        column: x => x.role_name,
                        principalTable: "app_roles",
                        principalColumn: "name",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "chat_dialogs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ai_assistant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_dialogs", x => x.id);
                    table.ForeignKey(
                        name: "FK_chat_dialogs_ai_assistants_ai_assistant_id",
                        column: x => x.ai_assistant_id,
                        principalTable: "ai_assistants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_chat_dialogs_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "telegram_link_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    token_hash_sha256_hex = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    expires_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    consumed_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true)
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

            migrationBuilder.CreateTable(
                name: "user_ai_assistant_extras",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ai_assistant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    values_json = table.Column<string>(type: "nvarchar(max)", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "user_bike_activities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    sport = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    start_time_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    total_seconds = table.Column<double>(type: "float", nullable: true),
                    distance_meters = table.Column<double>(type: "float", nullable: true),
                    calories = table.Column<double>(type: "float", nullable: true),
                    avg_heart_rate_bpm = table.Column<int>(type: "int", nullable: true),
                    max_heart_rate_bpm = table.Column<int>(type: "int", nullable: true),
                    intensity = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    trigger_method = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    imported_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    trackpoint_count = table.Column<int>(type: "int", nullable: false),
                    source_file_key = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    source_file_name = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_bike_activities", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_bike_activities_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_habits",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    category_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    is_mastered = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    days_to_master = table.Column<int>(type: "int", nullable: false, defaultValue: 21),
                    sort_order = table.Column<int>(type: "int", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_habits", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_habits_user_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "user_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_user_habits_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    role = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_roles", x => new { x.UserId, x.role });
                    table.ForeignKey(
                        name: "FK_user_roles_app_roles_role",
                        column: x => x.role,
                        principalTable: "app_roles",
                        principalColumn: "name",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_roles_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_todo_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    category_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: true),
                    done_date = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_todo_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_todo_items_user_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "user_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_user_todo_items_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_weekly_training_reviews",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ai_assistant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    period_from = table.Column<DateOnly>(type: "date", nullable: false),
                    period_to = table.Column<DateOnly>(type: "date", nullable: false),
                    data_fingerprint = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
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

            migrationBuilder.CreateTable(
                name: "user_weight_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    weight_kg = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "workout_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    session_code = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    day = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
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
                name: "chat_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    dialog_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    role = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_messages", x => x.id);
                    table.ForeignKey(
                        name: "FK_chat_messages_chat_dialogs_dialog_id",
                        column: x => x.dialog_id,
                        principalTable: "chat_dialogs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_bike_activity_trackpoints",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    activity_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    order_no = table.Column<int>(type: "int", nullable: false),
                    time_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    latitude = table.Column<double>(type: "float", nullable: true),
                    longitude = table.Column<double>(type: "float", nullable: true),
                    altitude_m = table.Column<double>(type: "float", nullable: true),
                    heart_rate_bpm = table.Column<int>(type: "int", nullable: true),
                    cadence = table.Column<int>(type: "int", nullable: true),
                    speed_m_s = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_bike_activity_trackpoints", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_bike_activity_trackpoints_user_bike_activities_activity_id",
                        column: x => x.activity_id,
                        principalTable: "user_bike_activities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_habit_checkins",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    habit_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "done"),
                    created_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false)
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
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "workout_exercises",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    session_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    meta = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    order_no = table.Column<int>(type: "int", nullable: false)
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
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    exercise_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    weight = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    reps = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    rpe = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    order_no = table.Column<int>(type: "int", nullable: false)
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
                name: "IX_ai_assistant_field_definitions_ai_assistant_id_field_key",
                table: "ai_assistant_field_definitions",
                columns: new[] { "ai_assistant_id", "field_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ai_assistant_field_definitions_ai_assistant_id_sort_order",
                table: "ai_assistant_field_definitions",
                columns: new[] { "ai_assistant_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "IX_ai_assistants_assistant_code",
                table: "ai_assistants",
                column: "assistant_code",
                unique: true,
                filter: "[assistant_code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ai_assistants_sort_order",
                table: "ai_assistants",
                column: "sort_order");

            migrationBuilder.CreateIndex(
                name: "IX_app_permissions_code",
                table: "app_permissions",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_app_permissions_sort_order",
                table: "app_permissions",
                column: "sort_order");

            migrationBuilder.CreateIndex(
                name: "IX_app_role_permissions_permission_code",
                table: "app_role_permissions",
                column: "permission_code");

            migrationBuilder.CreateIndex(
                name: "IX_app_roles_name",
                table: "app_roles",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_app_roles_sort_order",
                table: "app_roles",
                column: "sort_order");

            migrationBuilder.CreateIndex(
                name: "IX_chat_dialogs_ai_assistant_id",
                table: "chat_dialogs",
                column: "ai_assistant_id");

            migrationBuilder.CreateIndex(
                name: "IX_chat_dialogs_user_id_created_at_utc",
                table: "chat_dialogs",
                columns: new[] { "user_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_chat_messages_dialog_id_created_at_utc",
                table: "chat_messages",
                columns: new[] { "dialog_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_telegram_link_tokens_token_hash_sha256_hex",
                table: "telegram_link_tokens",
                column: "token_hash_sha256_hex",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_telegram_link_tokens_user_id_consumed_at_utc",
                table: "telegram_link_tokens",
                columns: new[] { "user_id", "consumed_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_user_ai_assistant_extras_ai_assistant_id",
                table: "user_ai_assistant_extras",
                column: "ai_assistant_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_ai_assistant_extras_user_id_ai_assistant_id",
                table: "user_ai_assistant_extras",
                columns: new[] { "user_id", "ai_assistant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_bike_activities_user_id_start_time_utc",
                table: "user_bike_activities",
                columns: new[] { "user_id", "start_time_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_user_bike_activity_trackpoints_activity_id_order_no",
                table: "user_bike_activity_trackpoints",
                columns: new[] { "activity_id", "order_no" });

            migrationBuilder.CreateIndex(
                name: "IX_user_categories_name",
                table: "user_categories",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_categories_sort_order",
                table: "user_categories",
                column: "sort_order");

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
                name: "IX_user_habits_category_id",
                table: "user_habits",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_habits_user_id_is_active",
                table: "user_habits",
                columns: new[] { "user_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "IX_user_habits_user_id_sort_order",
                table: "user_habits",
                columns: new[] { "user_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_role",
                table: "user_roles",
                column: "role");

            migrationBuilder.CreateIndex(
                name: "IX_user_todo_items_category_id",
                table: "user_todo_items",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_todo_items_user_id_done_date",
                table: "user_todo_items",
                columns: new[] { "user_id", "done_date" });

            migrationBuilder.CreateIndex(
                name: "IX_user_todo_items_user_id_due_date",
                table: "user_todo_items",
                columns: new[] { "user_id", "due_date" });

            migrationBuilder.CreateIndex(
                name: "IX_user_weekly_training_reviews_ai_assistant_id",
                table: "user_weekly_training_reviews",
                column: "ai_assistant_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_weekly_training_reviews_user_id_period_from_period_to",
                table: "user_weekly_training_reviews",
                columns: new[] { "user_id", "period_from", "period_to" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_weight_entries_user_id_date",
                table: "user_weight_entries",
                columns: new[] { "user_id", "date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_weight_entries_user_id_updated_at_utc",
                table: "user_weight_entries",
                columns: new[] { "user_id", "updated_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_users_selected_ai_assistant_id",
                table: "users",
                column: "selected_ai_assistant_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_telegram_chat_id",
                table: "users",
                column: "telegram_chat_id",
                unique: true,
                filter: "[telegram_chat_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_users_Username",
                table: "users",
                column: "Username",
                unique: true);

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
                name: "ai_assistant_field_definitions");

            migrationBuilder.DropTable(
                name: "app_role_permissions");

            migrationBuilder.DropTable(
                name: "chat_messages");

            migrationBuilder.DropTable(
                name: "telegram_link_tokens");

            migrationBuilder.DropTable(
                name: "user_ai_assistant_extras");

            migrationBuilder.DropTable(
                name: "user_bike_activity_trackpoints");

            migrationBuilder.DropTable(
                name: "user_habit_checkins");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "user_todo_items");

            migrationBuilder.DropTable(
                name: "user_weekly_training_reviews");

            migrationBuilder.DropTable(
                name: "user_weight_entries");

            migrationBuilder.DropTable(
                name: "workout_sets");

            migrationBuilder.DropTable(
                name: "app_permissions");

            migrationBuilder.DropTable(
                name: "chat_dialogs");

            migrationBuilder.DropTable(
                name: "user_bike_activities");

            migrationBuilder.DropTable(
                name: "user_habits");

            migrationBuilder.DropTable(
                name: "app_roles");

            migrationBuilder.DropTable(
                name: "workout_exercises");

            migrationBuilder.DropTable(
                name: "user_categories");

            migrationBuilder.DropTable(
                name: "workout_sessions");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "ai_assistants");
        }
    }
}
