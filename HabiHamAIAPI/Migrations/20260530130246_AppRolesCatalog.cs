using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HabiHamAIAPI.Migrations
{
    /// <inheritdoc />
    public partial class AppRolesCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "app_roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    is_system = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app_roles", x => x.Id);
                    table.UniqueConstraint("AK_app_roles_name", x => x.name);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_role",
                table: "user_roles",
                column: "role");

            migrationBuilder.CreateIndex(
                name: "IX_app_roles_name",
                table: "app_roles",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_app_roles_sort_order",
                table: "app_roles",
                column: "sort_order");

            migrationBuilder.Sql(
                """
                INSERT INTO app_roles ("Id", name, label, description, is_system, is_active, sort_order, created_at_utc, updated_at_utc)
                VALUES
                    ('11111111-1111-1111-1111-111111111101', 'Admin', 'Администратор', 'Полный доступ к админ-панели', TRUE, TRUE, 1, NOW() AT TIME ZONE 'UTC', NOW() AT TIME ZONE 'UTC'),
                    ('11111111-1111-1111-1111-111111111102', 'User', 'Пользователь', 'Базовый доступ к приложению', TRUE, TRUE, 2, NOW() AT TIME ZONE 'UTC', NOW() AT TIME ZONE 'UTC'),
                    ('11111111-1111-1111-1111-111111111103', 'AiUser', 'AI-пользователь', 'Доступ к разделу ИИ-помощника', TRUE, TRUE, 3, NOW() AT TIME ZONE 'UTC', NOW() AT TIME ZONE 'UTC')
                ON CONFLICT (name) DO NOTHING;
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO app_roles ("Id", name, label, description, is_system, is_active, sort_order, created_at_utc, updated_at_utc)
                SELECT gen_random_uuid(), ur.role, ur.role, NULL, FALSE, TRUE,
                       COALESCE((SELECT MAX(sort_order) FROM app_roles), 0) + ROW_NUMBER() OVER (ORDER BY ur.role),
                       NOW() AT TIME ZONE 'UTC', NOW() AT TIME ZONE 'UTC'
                FROM (
                    SELECT DISTINCT role FROM user_roles
                ) ur
                WHERE NOT EXISTS (
                    SELECT 1 FROM app_roles ar WHERE ar.name = ur.role
                );
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_user_roles_app_roles_role",
                table: "user_roles",
                column: "role",
                principalTable: "app_roles",
                principalColumn: "name",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_user_roles_app_roles_role",
                table: "user_roles");

            migrationBuilder.DropTable(
                name: "app_roles");

            migrationBuilder.DropIndex(
                name: "IX_user_roles_role",
                table: "user_roles");
        }
    }
}
