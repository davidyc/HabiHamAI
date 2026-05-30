namespace HabiHamAIAPI.Models;

public static class AppPermissionCatalog
{
    public const string Workouts = "app.workouts";
    public const string Bike = "app.bike";
    public const string Progress = "app.progress";
    public const string Habits = "app.habits";
    public const string Todos = "app.todos";
    public const string Profile = "app.profile";
    public const string Investments = "app.investments";
    public const string AiAssistant = "ai.assistant";
    public const string AdminUsers = "admin.users";
    public const string AdminRoles = "admin.roles";
    public const string AdminCategories = "admin.categories";
    public const string AdminProfiles = "admin.profiles";
    public const string AdminAiAssistants = "admin.ai_assistants";
    public const string AdminAiTestChat = "admin.ai_test_chat";
    public const string AdminDialogs = "admin.dialogs";

    public static readonly IReadOnlyList<AppPermissionSeed> All =
    [
        new(Workouts, "Силовые тренировки", "Программы, упражнения и журнал силовых тренировок", "app", 1),
        new(Bike, "Велотренировки", "Импорт TCX и история велозаездов", "app", 2),
        new(Progress, "Мой прогресс", "Дневник веса и график", "app", 3),
        new(Habits, "Привычки", "Ежедневные привычки и отметки", "app", 4),
        new(Todos, "Задачи", "Список задач и выполнение", "app", 5),
        new(Profile, "Профиль", "Личные данные и настройки", "app", 6),
        new(Investments, "Инвестиции", "Портфель и учёт инвестиционных позиций", "app", 7),
        new(AiAssistant, "AI помощник", "Чат с ИИ и связанные функции", "ai", 10),
        new(AdminUsers, "Учётные записи", "Создание и редактирование пользователей", "admin", 20),
        new(AdminRoles, "Роли", "Управление ролями и правами", "admin", 21),
        new(AdminCategories, "Категории", "Категории привычек и задач", "admin", 22),
        new(AdminProfiles, "Профили", "Просмотр профилей пользователей", "admin", 23),
        new(AdminAiAssistants, "ИИ-помощники", "Настройка ассистентов и полей", "admin", 24),
        new(AdminAiTestChat, "Тест чата", "Пробный чат из админки", "admin", 25),
        new(AdminDialogs, "Диалоги", "Диалоги пользователей", "admin", 26),
    ];

    public static readonly IReadOnlyList<string> AllCodes =
        All.Select(x => x.Code).ToList();

    public static readonly IReadOnlyList<string> DefaultAppPermissions =
        [Workouts, Bike, Progress, Habits, Todos, Profile, Investments];

    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> DefaultRolePermissions =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Admin"] = AllCodes,
            ["User"] = DefaultAppPermissions,
            ["AiUser"] = [.. DefaultAppPermissions, AiAssistant],
        };

    public static readonly IReadOnlyList<string> AdminRoleMinimumPermissions =
        [AdminUsers, AdminRoles];

    public sealed record AppPermissionSeed(
        string Code,
        string Label,
        string Description,
        string Category,
        int SortOrder);
}
