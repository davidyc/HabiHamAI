using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HabiHamAIAPI.Migrations
{
    /// <inheritdoc />
    public partial class AppRolePermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "app_permissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    category = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_system = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app_permissions", x => x.Id);
                    table.UniqueConstraint("AK_app_permissions_code", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "app_role_permissions",
                columns: table => new
                {
                    role_name = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    permission_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
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

            migrationBuilder.Sql(
                """
                INSERT INTO app_permissions ("Id", code, label, description, category, sort_order, is_system)
                VALUES
                    ('22222222-2222-2222-2222-222222222201', 'app.workouts', 'Тренировки', 'Раздел тренировок и силовых программ', 'app', 1, TRUE),
                    ('22222222-2222-2222-2222-222222222202', 'app.progress', 'Мой прогресс', 'Вес, велосипед и смежные метрики', 'app', 2, TRUE),
                    ('22222222-2222-2222-2222-222222222203', 'app.tracking', 'Трекинг', 'Привычки и задачи', 'app', 3, TRUE),
                    ('22222222-2222-2222-2222-222222222204', 'app.profile', 'Профиль', 'Личные данные и настройки', 'app', 4, TRUE),
                    ('22222222-2222-2222-2222-222222222210', 'ai.assistant', 'AI помощник', 'Чат с ИИ и связанные функции', 'ai', 10, TRUE),
                    ('22222222-2222-2222-2222-222222222220', 'admin.users', 'Учётные записи', 'Создание и редактирование пользователей', 'admin', 20, TRUE),
                    ('22222222-2222-2222-2222-222222222221', 'admin.roles', 'Роли', 'Управление ролями и правами', 'admin', 21, TRUE),
                    ('22222222-2222-2222-2222-222222222222', 'admin.categories', 'Категории', 'Категории привычек и задач', 'admin', 22, TRUE),
                    ('22222222-2222-2222-2222-222222222223', 'admin.profiles', 'Профили', 'Просмотр профилей пользователей', 'admin', 23, TRUE),
                    ('22222222-2222-2222-2222-222222222224', 'admin.ai_assistants', 'ИИ-помощники', 'Настройка ассистентов и полей', 'admin', 24, TRUE),
                    ('22222222-2222-2222-2222-222222222225', 'admin.ai_test_chat', 'Тест чата', 'Пробный чат из админки', 'admin', 25, TRUE),
                    ('22222222-2222-2222-2222-222222222226', 'admin.dialogs', 'Диалоги', 'Диалоги пользователей', 'admin', 26, TRUE)
                ON CONFLICT (code) DO NOTHING;
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO app_role_permissions (role_name, permission_code)
                SELECT 'Admin', code FROM app_permissions
                WHERE NOT EXISTS (SELECT 1 FROM app_role_permissions WHERE role_name = 'Admin');
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO app_role_permissions (role_name, permission_code)
                SELECT 'User', code FROM (VALUES
                    ('app.workouts'), ('app.progress'), ('app.tracking'), ('app.profile')
                ) AS v(code)
                WHERE EXISTS (SELECT 1 FROM app_roles WHERE name = 'User')
                  AND NOT EXISTS (SELECT 1 FROM app_role_permissions WHERE role_name = 'User');
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO app_role_permissions (role_name, permission_code)
                SELECT 'AiUser', code FROM (VALUES
                    ('app.workouts'), ('app.progress'), ('app.tracking'), ('app.profile'), ('ai.assistant')
                ) AS v(code)
                WHERE EXISTS (SELECT 1 FROM app_roles WHERE name = 'AiUser')
                  AND NOT EXISTS (SELECT 1 FROM app_role_permissions WHERE role_name = 'AiUser');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "app_role_permissions");

            migrationBuilder.DropTable(
                name: "app_permissions");
        }
    }
}
