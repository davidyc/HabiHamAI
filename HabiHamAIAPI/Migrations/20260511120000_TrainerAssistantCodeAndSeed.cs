using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HabiHamAIAPI.Migrations;

/// <summary>
/// Adds optional assistant_code on ai_assistants and seeds the built-in &quot;trainer&quot; assistant for workout chat.
/// </summary>
public partial class TrainerAssistantCodeAndSeed : Migration
{
    private static readonly Guid TrainerAssistantId = Guid.Parse("c3f89f42-7f91-4f8e-b9d3-a8f7e4d2c1b0");

    private static readonly Guid TrainerFieldWeightId = Guid.Parse("11111111-1111-4111-8111-111111111101");
    private static readonly Guid TrainerFieldSexId = Guid.Parse("11111111-1111-4111-8111-111111111102");
    private static readonly Guid TrainerFieldAgeId = Guid.Parse("11111111-1111-4111-8111-111111111103");
    private static readonly Guid TrainerFieldGoalId = Guid.Parse("11111111-1111-4111-8111-111111111104");
    private static readonly Guid TrainerFieldSkillId = Guid.Parse("11111111-1111-4111-8111-111111111105");
    private static readonly Guid TrainerFieldToolsId = Guid.Parse("11111111-1111-4111-8111-111111111106");

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "assistant_code",
            table: "ai_assistants",
            type: "character varying(64)",
            maxLength: 64,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_ai_assistants_assistant_code",
            table: "ai_assistants",
            column: "assistant_code",
            unique: true);

        migrationBuilder.Sql($@"
            INSERT INTO ai_assistants (id, assistant_code, name, description, system_prompt, settings_json, sort_order, is_active, created_at_utc)
            SELECT '{TrainerAssistantId}'::uuid,
                   'trainer',
                   'Тренер',
                   'Персональные программы и советы по тренировкам',
                   $$Ты — профессиональный фитнес-тренер и эксперт по биомеханике. Составь персональный совет или программу по профилю ниже.

### ДАННЫЕ ПОЛЬЗОВАТЕЛЯ:
- Вес: {{{{weight}}}} кг
- Пол: {{{{sex}}}}
- Возраст: {{{{age}}}} лет
- Цель: {{{{goal}}}}
- Опыт в тренировках: {{{{skill}}}}
- Доступное оборудование: {{{{tools}}}}

### ИНСТРУКЦИИ:
1. Используй только упражнения с тем инвентарём, что указан в {{{{tools}}}}.
2. Учитывай опыт ({{{{skill}}}}), пол, возраст и вес при объёме и интенсивности.
3. Ответ структурируй: краткий анализ, план (упражнение | подходы | повторения | отдых), техника безопасности.$$,
                   NULL,
                   -100,
                   true,
                   NOW() AT TIME ZONE 'utc'
            WHERE NOT EXISTS (SELECT 1 FROM ai_assistants WHERE assistant_code = 'trainer');
            ");

        SeedTrainerFieldDefinitions(migrationBuilder);
    }

    private static void SeedTrainerFieldDefinitions(MigrationBuilder migrationBuilder)
    {
        var tid = TrainerAssistantId.ToString();
        migrationBuilder.Sql($@"
INSERT INTO ai_assistant_field_definitions (id, ai_assistant_id, field_key, label, field_type, sort_order, is_required, created_at_utc)
SELECT '{TrainerFieldWeightId}', '{tid}'::uuid, 'weight', 'Вес (кг)', 'number', 0, true, NOW() AT TIME ZONE 'utc'
WHERE EXISTS (SELECT 1 FROM ai_assistants WHERE id = '{tid}'::uuid)
  AND NOT EXISTS (SELECT 1 FROM ai_assistant_field_definitions WHERE ai_assistant_id = '{tid}'::uuid AND field_key = 'weight');

INSERT INTO ai_assistant_field_definitions (id, ai_assistant_id, field_key, label, field_type, sort_order, is_required, created_at_utc)
SELECT '{TrainerFieldSexId}', '{tid}'::uuid, 'sex', 'Пол', 'text', 1, true, NOW() AT TIME ZONE 'utc'
WHERE EXISTS (SELECT 1 FROM ai_assistants WHERE id = '{tid}'::uuid)
  AND NOT EXISTS (SELECT 1 FROM ai_assistant_field_definitions WHERE ai_assistant_id = '{tid}'::uuid AND field_key = 'sex');

INSERT INTO ai_assistant_field_definitions (id, ai_assistant_id, field_key, label, field_type, sort_order, is_required, created_at_utc)
SELECT '{TrainerFieldAgeId}', '{tid}'::uuid, 'age', 'Возраст (лет)', 'text', 2, true, NOW() AT TIME ZONE 'utc'
WHERE EXISTS (SELECT 1 FROM ai_assistants WHERE id = '{tid}'::uuid)
  AND NOT EXISTS (SELECT 1 FROM ai_assistant_field_definitions WHERE ai_assistant_id = '{tid}'::uuid AND field_key = 'age');

INSERT INTO ai_assistant_field_definitions (id, ai_assistant_id, field_key, label, field_type, sort_order, is_required, created_at_utc)
SELECT '{TrainerFieldGoalId}', '{tid}'::uuid, 'goal', 'Цель', 'text', 3, true, NOW() AT TIME ZONE 'utc'
WHERE EXISTS (SELECT 1 FROM ai_assistants WHERE id = '{tid}'::uuid)
  AND NOT EXISTS (SELECT 1 FROM ai_assistant_field_definitions WHERE ai_assistant_id = '{tid}'::uuid AND field_key = 'goal');

INSERT INTO ai_assistant_field_definitions (id, ai_assistant_id, field_key, label, field_type, sort_order, is_required, created_at_utc)
SELECT '{TrainerFieldSkillId}', '{tid}'::uuid, 'skill', 'Опыт в тренировках', 'text', 4, true, NOW() AT TIME ZONE 'utc'
WHERE EXISTS (SELECT 1 FROM ai_assistants WHERE id = '{tid}'::uuid)
  AND NOT EXISTS (SELECT 1 FROM ai_assistant_field_definitions WHERE ai_assistant_id = '{tid}'::uuid AND field_key = 'skill');

INSERT INTO ai_assistant_field_definitions (id, ai_assistant_id, field_key, label, field_type, sort_order, is_required, created_at_utc)
SELECT '{TrainerFieldToolsId}', '{tid}'::uuid, 'tools', 'Доступное оборудование', 'textarea', 5, true, NOW() AT TIME ZONE 'utc'
WHERE EXISTS (SELECT 1 FROM ai_assistants WHERE id = '{tid}'::uuid)
  AND NOT EXISTS (SELECT 1 FROM ai_assistant_field_definitions WHERE ai_assistant_id = '{tid}'::uuid AND field_key = 'tools');
");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        var tid = TrainerAssistantId.ToString();
        migrationBuilder.Sql($"DELETE FROM ai_assistant_field_definitions WHERE ai_assistant_id = '{tid}'::uuid;");

        migrationBuilder.Sql("DELETE FROM ai_assistants WHERE assistant_code = 'trainer';");

        migrationBuilder.DropIndex(
            name: "IX_ai_assistants_assistant_code",
            table: "ai_assistants");

        migrationBuilder.DropColumn(
            name: "assistant_code",
            table: "ai_assistants");
    }
}
