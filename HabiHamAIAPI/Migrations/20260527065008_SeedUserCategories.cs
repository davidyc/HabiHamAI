using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HabiHamAIAPI.Migrations;

/// <summary>
/// Seeds default user_categories on databases that already applied
/// <see cref="AddCategoriesForHabitsAndTodos"/> before category seed SQL was added.
/// Idempotent: skips rows that already exist (by name).
/// </summary>
public partial class SeedUserCategories : Migration
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

    private static readonly Guid[] SeedCategoryIds =
    [
        CategoryHealthId,
        CategorySportId,
        CategoryNutritionId,
        CategorySleepId,
        CategoryWorkId,
        CategoryLearningId,
        CategoryHomeId,
        CategoryFinanceId,
        CategorySocialId,
        CategoryOtherId
    ];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
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
        var idList = string.Join(", ", Array.ConvertAll(SeedCategoryIds, x => $"'{x}'::uuid"));
        migrationBuilder.Sql($@"
DELETE FROM user_categories
WHERE id IN ({idList});
");
    }
}
