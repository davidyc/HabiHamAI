using Telegram.Bot.Types;

namespace HabiHamAIAPI.Services.Telegram;

/// <summary>Статичные подписи меню и команды бота (русский интерфейс).</summary>
internal static class TelegramBotMenu
{
    internal const string BtnHelp = "Помощь";
    internal const string BtnHideKeyboard = "Скрыть клавиатуру";
    internal const string BtnSendWeight = "Отправить вес";
    internal const string BtnCancelWeight = "Отмена";

    internal static readonly IReadOnlyList<BotCommand> BotCommands =
    [
        new() { Command = "start", Description = "Главное меню" },
        new() { Command = "weight", Description = "Отправить вес" },
        new() { Command = "help", Description = "Справка" },
        new() { Command = "keyboard", Description = "Показать кнопки меню" },
        new() { Command = "hide", Description = "Скрыть кнопки" },
    ];

    internal static readonly string[][] MainKeyboardRows =
    [
        [BtnSendWeight],
        [BtnHelp, BtnHideKeyboard],
    ];

    /// <summary>Пока ждём число — одна кнопка отмены.</summary>
    internal static readonly string[][] WeightInputKeyboardRows = [[BtnCancelWeight]];

    internal const string WeightPrompt =
        "Введите вес в килограммах одним числом (например <b>72.5</b> или <b>72,5</b>). Другие сообщения сейчас не принимаются.\n\n"
        + "Нажмите «Отмена», чтобы выйти.";

    internal const string Welcome =
        "Здравствуйте! Я бот HabiHamAI.\n\n"
        + "Откройте команды через меню (☰ слева от поля ввода) или нажмите кнопки ниже.";

    internal const string Help =
        "Доступно:\n"
        + "• /start — приветствие и кнопки меню\n"
        + "• /weight или кнопка «Отправить вес» — ввод веса (затем только число в кг)\n"
        + "• /help — эта справка\n"
        + "• /keyboard — снова показать кнопки\n"
        + "• /hide — убрать кнопки с экрана\n\n"
        + "В обычном режиме произвольный текст повторяется обратно.";
}
