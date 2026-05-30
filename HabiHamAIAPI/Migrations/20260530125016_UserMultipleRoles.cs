using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HabiHamAIAPI.Migrations
{
    /// <inheritdoc />
    public partial class UserMultipleRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_roles", x => new { x.UserId, x.role });
                    table.ForeignKey(
                        name: "FK_user_roles_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO user_roles ("UserId", role)
                SELECT "Id", "Role"
                FROM users
                WHERE "Role" IS NOT NULL AND TRIM("Role") <> '';
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO user_roles ("UserId", role)
                SELECT u."Id", 'User'
                FROM users u
                WHERE NOT EXISTS (
                    SELECT 1 FROM user_roles ur WHERE ur."UserId" = u."Id"
                );
                """);

            migrationBuilder.DropColumn(
                name: "Role",
                table: "users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "users",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "User");

            migrationBuilder.Sql(
                """
                UPDATE users u
                SET "Role" = COALESCE(
                    (
                        SELECT ur.role
                        FROM user_roles ur
                        WHERE ur."UserId" = u."Id"
                        ORDER BY CASE ur.role
                            WHEN 'Admin' THEN 1
                            WHEN 'AiUser' THEN 2
                            ELSE 3
                        END
                        LIMIT 1
                    ),
                    'User'
                );
                """);

            migrationBuilder.DropTable(
                name: "user_roles");
        }
    }
}
