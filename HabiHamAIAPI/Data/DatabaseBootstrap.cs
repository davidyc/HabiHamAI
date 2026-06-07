using HabiHamAIAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace HabiHamAIAPI.Data;

public static class DatabaseBootstrap
{
    private static readonly Guid TrainerAssistantId = Guid.Parse("c3f89f42-7f91-4f8e-b9d3-a8f7e4d2c1b0");

    private static readonly (Guid Id, string FieldKey, string Label, string FieldType, int SortOrder)[] TrainerFieldDefinitions =
    [
        (Guid.Parse("11111111-1111-4111-8111-111111111101"), "weight", "Вес (кг)", "number", 0),
        (Guid.Parse("11111111-1111-4111-8111-111111111108"), "height", "Рост (см)", "number", 1),
        (Guid.Parse("11111111-1111-4111-8111-111111111102"), "sex", "Пол", "text", 2),
        (Guid.Parse("11111111-1111-4111-8111-111111111103"), "age", "Возраст (лет)", "text", 3),
        (Guid.Parse("11111111-1111-4111-8111-111111111104"), "goal", "Цель", "text", 4),
        (Guid.Parse("11111111-1111-4111-8111-111111111105"), "skill", "Опыт в тренировках", "text", 5),
        (Guid.Parse("11111111-1111-4111-8111-111111111106"), "tools", "Доступное оборудование", "textarea", 6),
    ];

    private const string TrainerSystemPrompt =
        """
        Ты — профессиональный фитнес-тренер и эксперт по биомеханике. Составь персональный совет или программу по профилю ниже.

        ### ДАННЫЕ ПОЛЬЗОВАТЕЛЯ:
        - Вес: {{weight}} кг
        - Рост: {{height}} см
        - Пол: {{sex}}
        - Возраст: {{age}} лет
        - Цель: {{goal}}
        - Опыт в тренировках: {{skill}}
        - Доступное оборудование: {{tools}}

        ### ИНСТРУКЦИИ:
        1. Используй только упражнения с тем инвентарём, что указан в {{tools}}.
        2. Учитывай опыт ({{skill}}), пол, возраст и вес при объёме и интенсивности.
        3. Ответ структурируй: краткий анализ, план (упражнение | подходы | повторения | отдых), техника безопасности.
        """;

    private static readonly (Guid Id, string Name, string Description, int SortOrder)[] DefaultUserCategories =
    [
        (Guid.Parse("a1000001-0001-4001-8001-000000000001"), "Здоровье", "Привычки и дела про здоровье и самочувствие", 1),
        (Guid.Parse("a1000001-0001-4001-8001-000000000002"), "Спорт и фитнес", "Тренировки, активность, движение", 2),
        (Guid.Parse("a1000001-0001-4001-8001-000000000003"), "Питание", "Рацион, вода, режим питания", 3),
        (Guid.Parse("a1000001-0001-4001-8001-000000000004"), "Сон и восстановление", "Сон, отдых, восстановление", 4),
        (Guid.Parse("a1000001-0001-4001-8001-000000000005"), "Работа", "Задачи и привычки, связанные с работой", 5),
        (Guid.Parse("a1000001-0001-4001-8001-000000000006"), "Обучение", "Развитие, курсы, чтение", 6),
        (Guid.Parse("a1000001-0001-4001-8001-000000000007"), "Дом и быт", "Быт, уборка, домашние дела", 7),
        (Guid.Parse("a1000001-0001-4001-8001-000000000008"), "Финансы", "Бюджет, накопления, финансовые привычки", 8),
        (Guid.Parse("a1000001-0001-4001-8001-000000000009"), "Социальное", "Общение, семья, отношения", 9),
        (Guid.Parse("a1000001-0001-4001-8001-00000000000a"), "Прочее", "Всё, что не попало в другие категории", 10),
    ];

    public static async Task EnsureTrainerAssistantAsync(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var trainer = await dbContext.AiAssistants
            .FirstOrDefaultAsync(x => x.AssistantCode == "trainer", cancellationToken);

        if (trainer is null)
        {
            trainer = new AiAssistant
            {
                Id = TrainerAssistantId,
                AssistantCode = "trainer",
                Name = "Тренер",
                Description = "Персональные программы и советы по тренировкам",
                SystemPrompt = TrainerSystemPrompt,
                SettingsJson = null,
                SortOrder = -100,
                IsActive = true,
                IsSystem = true,
                CreatedAtUtc = now,
            };
            dbContext.AiAssistants.Add(trainer);
        }
        else
        {
            trainer.Name = "Тренер";
            trainer.Description = "Персональные программы и советы по тренировкам";
            trainer.SystemPrompt = TrainerSystemPrompt;
            trainer.SortOrder = -100;
            trainer.IsActive = true;
            trainer.IsSystem = true;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var existingFields = await dbContext.AiAssistantFieldDefinitions
            .Where(x => x.AiAssistantId == trainer.Id)
            .ToListAsync(cancellationToken);

        foreach (var (id, fieldKey, label, fieldType, sortOrder) in TrainerFieldDefinitions)
        {
            var field = existingFields.FirstOrDefault(x => x.FieldKey == fieldKey);
            if (field is null)
            {
                dbContext.AiAssistantFieldDefinitions.Add(new AiAssistantFieldDefinition
                {
                    Id = id,
                    AiAssistantId = trainer.Id,
                    FieldKey = fieldKey,
                    Label = label,
                    FieldType = fieldType,
                    SortOrder = sortOrder,
                    IsRequired = true,
                    IsSystem = true,
                    CreatedAtUtc = now,
                });
            }
            else
            {
                field.Label = label;
                field.FieldType = fieldType;
                field.SortOrder = sortOrder;
                field.IsRequired = true;
                field.IsSystem = true;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public static async Task EnsureDefaultUserCategoriesAsync(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        foreach (var (id, name, description, sortOrder) in DefaultUserCategories)
        {
            var existing = await dbContext.UserCategories
                .FirstOrDefaultAsync(x => x.Name == name, cancellationToken);
            if (existing is null)
            {
                dbContext.UserCategories.Add(new UserCategory
                {
                    Id = id,
                    Name = name,
                    Description = description,
                    IsActive = true,
                    SortOrder = sortOrder,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                });
            }
            else
            {
                existing.Description = description;
                existing.IsActive = true;
                existing.SortOrder = sortOrder;
                existing.UpdatedAtUtc = now;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
