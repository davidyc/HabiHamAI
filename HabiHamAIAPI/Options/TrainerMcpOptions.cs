namespace HabiHamAIAPI.Options;

public sealed class TrainerMcpOptions
{
    public bool Enabled { get; set; } = true;

    /// <summary>HTTP endpoint for external MCP clients (Cursor). Requires JWT.</summary>
    public bool HttpEndpointEnabled { get; set; } = true;

    public string HttpPath { get; set; } = "/api/mcp/trainer";

    public int MaxAgentRounds { get; set; } = 6;

    public int MaxToolCallsPerChat { get; set; } = 12;

    public int DefaultHistoryDays { get; set; } = 90;

    public int MaxStrengthSessions { get; set; } = 15;

    public int MaxBikeActivities { get; set; } = 15;

    public int MaxWeightEntries { get; set; } = 60;

    public int MaxPrograms { get; set; } = 10;

    /// <summary>Дней в периоде сводки по умолчанию.</summary>
    public int DefaultTrainingSummaryDays { get; set; } = 7;

    public int MaxTrainingSummaryDays { get; set; } = 14;
}
