using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace HabiHamAIAPI.Services.Telegram;

/// <summary>Статичные подписи меню и команды бота (русский интерфейс).</summary>
internal static class TelegramBotMenu
{
    /// <summary>Основные кнопки меню (остальное — команды в ☰).</summary>
    internal const string BtnSendWeight = "⚖️  Записать вес";

    internal const string BtnImportTcx = "🚴  Импорт TCX";

    internal const string BtnCancelWeight = "↩️  Отмена";

    internal static readonly IReadOnlyList<BotCommand> BotCommands =
    [
        new() { Command = "start", Description = "Главное меню и кнопка" },
        new() { Command = "weight", Description = "Записать вес" },
        new() { Command = "tcx", Description = "Как импортировать велозаезд (.tcx)" },
        new() { Command = "help", Description = "Справка по боту" },
        new() { Command = "keyboard", Description = "Показать кнопку меню" },
        new() { Command = "hide", Description = "Скрыть клавиатуру" },
    ];

    internal static readonly ReplyKeyboardMarkup MainKeyboard = new(
        [
            [new KeyboardButton(BtnSendWeight)],
            [new KeyboardButton(BtnImportTcx)],
        ])
    {
        ResizeKeyboard = true,
        IsPersistent = true,
        InputFieldPlaceholder = "Команды — в меню слева от поля ввода",
    };

    /// <summary>Пока ждём число — одна кнопка отмены.</summary>
    internal static readonly ReplyKeyboardMarkup WeightInputKeyboard = new(
        [[new KeyboardButton(BtnCancelWeight)]])
    {
        ResizeKeyboard = true,
        InputFieldPlaceholder = "Введите вес в кг, например 72.5",
    };

    internal const string WeightPrompt =
        "Введите вес в килограммах <b>одним числом</b> (например <b>72.5</b> или <b>72,5</b>).\n\n"
        + "Другие сообщения сейчас не принимаются. Нажмите кнопку отмены ниже, чтобы выйти.";

    internal const string ImportTcxHint =
        "<b>Импорт велотренировки (TCX)</b>\n"
        + "───────────────\n"
        + "Пришлите в этот чат файл с расширением <b>.tcx</b> (как в веб-приложении, раздел «Велотренировки»).\n\n"
        + "Условия: аккаунт привязан к Telegram; в файле активность со спортом <b>Biking</b> (велосипед). "
        + "Лимит размера файла в Telegram — до 20 МБ.";

    internal const string Welcome =
        "✨ <b>HabiHamAI</b>\n"
        + "───────────────\n"
        + "• Дневник веса — кнопка ниже 👇\n"
        + "• Велозаезд — отправьте файл <b>.tcx</b> в чат (или кнопка «Импорт TCX» / команда /tcx)\n\n"
        + "<i>Совет:</i> команды и справка — в меню <b>☰</b> слева от поля ввода.\n\n"
        + "Если аккаунт ещё не привязан, сделайте это в приложении: профиль → «Подключить Telegram».";

    internal const string Help =
        "<b>Что умеет бот</b>\n"
        + "───────────────\n"
        + "• «Записать вес» или /weight — запись веса в дневник (нужна привязка аккаунта)\n"
        + "• Отправка файла <b>.tcx</b> — импорт велотренировки (те же правила, что в приложении)\n"
        + "• /tcx — напоминание, как импортировать велозаезд\n"
        + "• /start — приветствие и кнопки меню\n"
        + "• /keyboard — снова показать кнопки\n"
        + "• /hide — убрать клавиатуру\n\n"
        + "В обычном режиме произвольный текст бот просто повторяет.";
}
