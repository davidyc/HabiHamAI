using HabiHamAIAPI.Data;
using HabiHamAIDataMigrator;
using Microsoft.EntityFrameworkCore;

Console.WriteLine("HabiHamAI: миграция SQL Server → SQL Server");
Console.WriteLine("============================================");
Console.WriteLine();

await using var sourceContext = CreateContext(MigrationSettings.SourceConnectionString);
await using var targetContext = CreateContext(MigrationSettings.TargetConnectionString);

var connectionCheck = await DatabaseConnectionChecker.CheckAsync(sourceContext, targetContext);
if (!connectionCheck.Success)
{
    return 1;
}

Console.WriteLine();
Console.WriteLine("Запуск миграции данных...");
Console.WriteLine();

var migrator = new SqlServerToSqlServerMigrator(
    sourceContext,
    targetContext,
    MigrationSettings.UsernamesToMigrate);

try
{
    await migrator.RunAsync();
    return 0;
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"Ошибка миграции: {ex.Message}");
    Console.ResetColor();
    if (ex.InnerException is not null)
    {
        Console.WriteLine($"  {ex.InnerException.Message}");
    }

    return 1;
}

static AppDbContext CreateContext(string connectionString)
{
    var options = new DbContextOptionsBuilder<AppDbContext>()
        .UseSqlServer(connectionString)
        .Options;
    return new AppDbContext(options);
}
