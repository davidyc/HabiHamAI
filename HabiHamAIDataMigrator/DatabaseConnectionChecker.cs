using HabiHamAIAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace HabiHamAIDataMigrator;

internal sealed record ConnectionCheckResult(
    bool Success,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Details);

internal static class DatabaseConnectionChecker
{
    public static async Task<ConnectionCheckResult> CheckAsync(
        AppDbContext source,
        AppDbContext target,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var details = new List<string>();

        Console.WriteLine("Проверка подключений...");
        Console.WriteLine();

        await CheckSqlServerAsync(source, "источник", details, errors, cancellationToken);
        Console.WriteLine();
        await CheckSqlServerAsync(target, "цель", details, errors, cancellationToken);

        Console.WriteLine();
        if (errors.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Все подключения успешны.");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Проверка подключений не пройдена:");
            foreach (var error in errors)
            {
                Console.WriteLine($"  • {error}");
            }

            Console.ResetColor();
        }

        return new ConnectionCheckResult(errors.Count == 0, errors, details);
    }

    private static async Task CheckSqlServerAsync(
        AppDbContext context,
        string label,
        List<string> details,
        List<string> errors,
        CancellationToken cancellationToken)
    {
        Console.Write($"  SQL Server ({label}) ... ");

        try
        {
            await context.Database.OpenConnectionAsync(cancellationToken);

            var version = await context.Database
                .SqlQueryRaw<string>("SELECT @@VERSION AS [Value]")
                .FirstAsync(cancellationToken);

            var dbName = context.Database.GetDbConnection().Database;
            var dataSource = context.Database.GetDbConnection().DataSource;
            var usersTableExists = await TableExistsAsync(context, "users", cancellationToken);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("OK");
            Console.ResetColor();

            details.Add($"SQL Server ({label}): {dataSource}/{dbName}");
            details.Add($"  {Truncate(version.Split('\n')[0], 120)}");
            details.Add(usersTableExists
                ? "  таблица users: найдена"
                : "  таблица users: не найдена");

            Console.WriteLine($"    сервер: {dataSource}");
            Console.WriteLine($"    база:   {dbName}");

            if (!usersTableExists)
            {
                errors.Add($"SQL Server ({label}): таблица users не найдена — проверьте схему БД.");
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("ОШИБКА");
            Console.ResetColor();
            errors.Add($"SQL Server ({label}): {ex.Message}");
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }
    }

    private static async Task<bool> TableExistsAsync(
        AppDbContext context,
        string tableName,
        CancellationToken cancellationToken) =>
        await context.Database
            .SqlQueryRaw<int>(
                """
                SELECT COUNT(*) AS [Value]
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_NAME = {0}
                """,
                tableName)
            .FirstAsync(cancellationToken) > 0;

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength] + "...";
}
