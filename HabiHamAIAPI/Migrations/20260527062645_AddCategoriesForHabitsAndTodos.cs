using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HabiHamAIAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoriesForHabitsAndTodos : Migration
    {
        private static readonly Guid CategoryHealthId = Guid.Parse("a1000001-0001-4001-8001-000000000001");
        private static readonly Guid CategorySportId = Guid.Parse("a1000001-0001-4001-8001-000000000002");
        private static readonly Guid CategoryNutritionId = Guid.Parse("a1000001-0001-4001-8001-000000000003");
        private static readonly Guid CategorySleepId = Guid.Parse("a1000001-0001-4001-8001-000000000004");
        private static readonly Guid CategoryWorkId = Guid.Parse("a1000001-0001-4001-8001-000000000005");
        private static readonly Guid CategoryLearningId = Guid.Parse("a1000001-0001-4001-8001-000000000006");
        private static readonly Guid CategoryHomeId = Guid.Parse("a1000001-0001-4001-8001-000000000007");
        private static readonly Guid CategoryFinanceId = Guid.Parse("a1000001-0001-4001-8001-000000000008");
        private static readonly Guid CategorySocialId = Guid.Parse("a1000001-0001-4001-8001-000000000009");
        private static readonly Guid CategoryOtherId = Guid.Parse("a1000001-0001-4001-8001-00000000000a");

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "category_id",
                table: "user_todo_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "category_id",
                table: "user_habits",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "user_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_categories", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_todo_items_category_id",
                table: "user_todo_items",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_habits_category_id",
                table: "user_habits",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_categories_name",
                table: "user_categories",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_categories_sort_order",
                table: "user_categories",
                column: "sort_order");

            migrationBuilder.AddForeignKey(
                name: "FK_user_habits_user_categories_category_id",
                table: "user_habits",
                column: "category_id",
                principalTable: "user_categories",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_user_todo_items_user_categories_category_id",
                table: "user_todo_items",
                column: "category_id",
                principalTable: "user_categories",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            SeedUserCategories(migrationBuilder);
        }

        private static void SeedUserCategories(MigrationBuilder migrationBuilder)
        {
            var utcNow = "NOW() AT TIME ZONE 'utc'";
            var rows = new (Guid Id, string Name, string Description, int SortOrder)[]
            {
                (CategoryHealthId, "Здоровье", "Привычки и дела про здоровье и самочувствие", 1),
                (CategorySportId, "Спорт и фитнес", "Тренировки, активность, движение", 2),
                (CategoryNutritionId, "Питание", "Рацион, вода, режим питания", 3),
                (CategorySleepId, "Сон и восстановление", "Сон, отдых, восстановление", 4),
                (CategoryWorkId, "Работа", "Задачи и привычки, связанные с работой", 5),
                (CategoryLearningId, "Обучение", "Развитие, курсы, чтение", 6),
                (CategoryHomeId, "Дом и быт", "Быт, уборка, домашние дела", 7),
                (CategoryFinanceId, "Финансы", "Бюджет, накопления, финансовые привычки", 8),
                (CategorySocialId, "Социальное", "Общение, семья, отношения", 9),
                (CategoryOtherId, "Прочее", "Всё, что не попало в другие категории", 10),
            };

            foreach (var (id, name, description, sortOrder) in rows)
            {
                var escapedName = name.Replace("'", "''");
                var escapedDescription = description.Replace("'", "''");
                migrationBuilder.Sql($@"
INSERT INTO user_categories (id, name, description, is_active, sort_order, created_at_utc, updated_at_utc)
SELECT '{id}'::uuid, '{escapedName}', '{escapedDescription}', true, {sortOrder}, {utcNow}, {utcNow}
WHERE NOT EXISTS (SELECT 1 FROM user_categories WHERE name = '{escapedName}');
");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_user_habits_user_categories_category_id",
                table: "user_habits");

            migrationBuilder.DropForeignKey(
                name: "FK_user_todo_items_user_categories_category_id",
                table: "user_todo_items");

            migrationBuilder.DropTable(
                name: "user_categories");

            migrationBuilder.DropIndex(
                name: "IX_user_todo_items_category_id",
                table: "user_todo_items");

            migrationBuilder.DropIndex(
                name: "IX_user_habits_category_id",
                table: "user_habits");

            migrationBuilder.DropColumn(
                name: "category_id",
                table: "user_todo_items");

            migrationBuilder.DropColumn(
                name: "category_id",
                table: "user_habits");
        }
    }
}
