using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HabiHamAIAPI.Migrations
{
    /// <inheritdoc />
    public partial class InvestmentsPermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO app_permissions ("Id", code, label, description, category, sort_order, is_system)
                VALUES
                    ('44444444-4444-4444-4444-444444444401', 'app.investments', 'Инвестиции', 'Портфель и учёт инвестиционных позиций', 'app', 7, TRUE)
                ON CONFLICT (code) DO NOTHING;
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO app_role_permissions (role_name, permission_code)
                SELECT role_name, 'app.investments'
                FROM (VALUES ('Admin'), ('User'), ('AiUser')) AS roles(role_name)
                WHERE NOT EXISTS (
                    SELECT 1 FROM app_role_permissions x
                    WHERE x.role_name = roles.role_name AND x.permission_code = 'app.investments'
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
