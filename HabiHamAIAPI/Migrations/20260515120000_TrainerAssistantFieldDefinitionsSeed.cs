using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HabiHamAIAPI.Migrations;

/// <summary>
/// Seeds ai_assistant_field_definitions for the built-in trainer assistant on databases that already applied
/// <see cref="TrainerAssistantCodeAndSeed"/> before field definitions were added.
/// Idempotent: skips rows that already exist.
/// </summary>
public partial class TrainerAssistantFieldDefinitionsSeed : Migration
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
        migrationBuilder.Sql($@"
DELETE FROM ai_assistant_field_definitions
WHERE ai_assistant_id = '{tid}'::uuid
  AND field_key IN ('weight', 'sex', 'age', 'goal', 'skill', 'tools')
  AND id IN (
    '{TrainerFieldWeightId}', '{TrainerFieldSexId}', '{TrainerFieldAgeId}',
    '{TrainerFieldGoalId}', '{TrainerFieldSkillId}', '{TrainerFieldToolsId}'
  );
");
    }
}
