namespace HabiHamAIDataMigrator;

/// <summary>Настройки миграции — укажите строки подключения и логины пользователей.</summary>
internal static class MigrationSettings
{
    /// <summary>SQL Server — источник (откуда копируем).</summary>
    public const string SourceConnectionString =
        "Server=SOURCE_SERVER;Database=habiham;User Id=...;Password=...;Encrypt=True;TrustServerCertificate=True";

    /// <summary>SQL Server — цель (куда копируем).</summary>
    public const string TargetConnectionString =
        "Server=localhost;Database=habiham_db;User Id=sa;Password=...;TrustServerCertificate=True";

    /// <summary>
    /// Пользователи для миграции (сопоставление по Username, без учёта регистра).                                                                                    
    /// Сначала проверяется наличие в источнике и цели, затем создаются отсутствующие в цели.
    /// </summary>
    public static readonly string[] UsernamesToMigrate = ["admin"];

    /// <summary>Размер пакета для SaveChanges при больших таблицах (трекпоинты).</summary>
    public const int BatchSize = 500;
}
