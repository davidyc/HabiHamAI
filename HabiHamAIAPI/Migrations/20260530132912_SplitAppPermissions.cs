using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HabiHamAIAPI.Migrations
{
    /// <inheritdoc />
    public partial class SplitAppPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO app_permissions ("Id", code, label, description, category, sort_order, is_system)
                VALUES
                    ('33333333-3333-3333-3333-333333333301', 'app.bike', 'Велотренировки', 'Импорт TCX и история велозаездов', 'app', 2, TRUE),
                    ('33333333-3333-3333-3333-333333333302', 'app.habits', 'Привычки', 'Ежедневные привычки и отметки', 'app', 4, TRUE),
                    ('33333333-3333-3333-3333-333333333303', 'app.todos', 'Задачи', 'Список задач и выполнение', 'app', 5, TRUE)
                ON CONFLICT (code) DO NOTHING;
                """);

            migrationBuilder.Sql(
                """
                UPDATE app_permissions
                SET label = 'Силовые тренировки',
                    description = 'Программы, упражнения и журнал силовых тренировок',
                    sort_order = 1
                WHERE code = 'app.workouts';

                UPDATE app_permissions
                SET description = 'Дневник веса и график',
                    sort_order = 3
                WHERE code = 'app.progress';

                UPDATE app_permissions
                SET sort_order = 6
                WHERE code = 'app.profile';
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO app_role_permissions (role_name, permission_code)
                SELECT DISTINCT rp.role_name, 'app.habits'
                FROM app_role_permissions rp
                WHERE rp.permission_code = 'app.tracking'
                  AND NOT EXISTS (
                      SELECT 1 FROM app_role_permissions x
                      WHERE x.role_name = rp.role_name AND x.permission_code = 'app.habits'
                  );

                INSERT INTO app_role_permissions (role_name, permission_code)
                SELECT DISTINCT rp.role_name, 'app.todos'
                FROM app_role_permissions rp
                WHERE rp.permission_code = 'app.tracking'
                  AND NOT EXISTS (
                      SELECT 1 FROM app_role_permissions x
                      WHERE x.role_name = rp.role_name AND x.permission_code = 'app.todos'
                  );

                INSERT INTO app_role_permissions (role_name, permission_code)
                SELECT DISTINCT rp.role_name, 'app.bike'
                FROM app_role_permissions rp
                WHERE rp.permission_code IN ('app.progress', 'app.workouts', 'app.tracking')
                  AND NOT EXISTS (
                      SELECT 1 FROM app_role_permissions x
                      WHERE x.role_name = rp.role_name AND x.permission_code = 'app.bike'
                  );

                INSERT INTO app_role_permissions (role_name, permission_code)
                SELECT 'Admin', code
                FROM app_permissions
                WHERE code IN ('app.bike', 'app.habits', 'app.todos')
                  AND NOT EXISTS (
                      SELECT 1 FROM app_role_permissions x
                      WHERE x.role_name = 'Admin' AND x.permission_code = app_permissions.code
                  );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
