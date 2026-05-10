using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HabiHamAIAPI.Migrations;

/// <summary>
/// Adds <c>is_system</c> flags for built-in trainer assistant and its field definitions (cannot delete via API).
/// Ensures trainer has &quot;height&quot; extra field and prompt line (idempotent if earlier migrations applied).
/// </summary>
public partial class SystemAiFlagsAndTrainerHeightEnsure : Migration
{
    private static readonly Guid TrainerAssistantId = Guid.Parse("c3f89f42-7f91-4f8e-b9d3-a8f7e4d2c1b0");
    private static readonly Guid TrainerFieldHeightId = Guid.Parse("11111111-1111-4111-8111-111111111108");

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "is_system",
            table: "ai_assistants",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "is_system",
            table: "ai_assistant_field_definitions",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        var tid = TrainerAssistantId.ToString();
        migrationBuilder.Sql($@"
UPDATE ai_assistants SET is_system = true WHERE assistant_code = 'trainer';

UPDATE ai_assistant_field_definitions SET is_system = true WHERE ai_assistant_id = '{tid}'::uuid;

INSERT INTO ai_assistant_field_definitions (id, ai_assistant_id, field_key, label, field_type, sort_order, is_required, created_at_utc, is_system)
SELECT '{TrainerFieldHeightId}', '{tid}'::uuid, 'height', 'Рост (см)', 'number', 1, true, NOW() AT TIME ZONE 'utc', true
WHERE EXISTS (SELECT 1 FROM ai_assistants WHERE id = '{tid}'::uuid)
  AND NOT EXISTS (SELECT 1 FROM ai_assistant_field_definitions WHERE ai_assistant_id = '{tid}'::uuid AND field_key = 'height');

UPDATE ai_assistant_field_definitions SET is_system = true WHERE ai_assistant_id = '{tid}'::uuid;

UPDATE ai_assistant_field_definitions SET sort_order = 0 WHERE ai_assistant_id = '{tid}'::uuid AND field_key = 'weight';
UPDATE ai_assistant_field_definitions SET sort_order = 1 WHERE ai_assistant_id = '{tid}'::uuid AND field_key = 'height';
UPDATE ai_assistant_field_definitions SET sort_order = 2 WHERE ai_assistant_id = '{tid}'::uuid AND field_key = 'sex';
UPDATE ai_assistant_field_definitions SET sort_order = 3 WHERE ai_assistant_id = '{tid}'::uuid AND field_key = 'age';
UPDATE ai_assistant_field_definitions SET sort_order = 4 WHERE ai_assistant_id = '{tid}'::uuid AND field_key = 'goal';
UPDATE ai_assistant_field_definitions SET sort_order = 5 WHERE ai_assistant_id = '{tid}'::uuid AND field_key = 'skill';
UPDATE ai_assistant_field_definitions SET sort_order = 6 WHERE ai_assistant_id = '{tid}'::uuid AND field_key = 'tools';

UPDATE ai_assistants
SET system_prompt = REPLACE(system_prompt, '- Пол:', '- Рост: {{{{height}}}} см
- Пол:')
WHERE assistant_code = 'trainer'
  AND position('- Рост:' in system_prompt) = 0;
");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "is_system",
            table: "ai_assistant_field_definitions");

        migrationBuilder.DropColumn(
            name: "is_system",
            table: "ai_assistants");
    }
}
