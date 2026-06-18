using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace HabiHamAIAPI.Services.Telegram;

/// <summary>Статичные подписи меню и команды бота (русский интерфейс).</summary>
internal static class TelegramBotMenu
{
    /// <summary>Основные кнопки меню (остальное — команды в ☰).</summary>
    internal const string BtnSendWeight = "⚖️  Записать вес";

    internal const string BtnImportTcx = "🚴  Импорт TCX";

    internal const string BtnTrainer = "🤖  Спросить тренера";

    internal const string BtnCancelWeight = "↩️  Отмена";

    internal static readonly IReadOnlyList<BotCommand> BotCommands =
    [
        new() { Command = "start", Description = "Главное меню и кнопка" },
        new() { Command = "trainer", Description = "Чат с AI-тренером" },
        new() { Command = "new", Description = "Новый диалог с тренером" },
        new() { Command = "weight", Description = "Записать вес" },
        new() { Command = "tcx", Description = "Как импортировать велозаезд (.tcx)" },
        new() { Command = "help", Description = "Справка по боту" },
        new() { Command = "keyboard", Description = "Показать кнопку меню" },
        new() { Command = "hide", Description = "Скрыть клавиатуру" },
    ];

    internal static readonly ReplyKeyboardMarkup MainKeyboard = new(
        [
            [new KeyboardButton(BtnSendWeight), new KeyboardButton(BtnTrainer)],
            [new KeyboardButton(BtnImportTcx)],
        ])
    {
        ResizeKeyboard = true,
        IsPersistent = true,
        InputFieldPlaceholder = "Сообщение тренеру или команды в меню ☰",
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

    internal const string TrainerIntro =
        "<b>AI-тренер</b>\n"
        + "───────────────\n"
        + "Задайте вопрос о тренировках, программе, прогрессе или технике — ответ будет с учётом ваших данных из приложения.\n\n"
        + "Просто напишите сообщение в чат. /new — начать новый диалог с тренером.";

    internal const string TrainerNewDialog =
        "Начат новый диалог с AI-тренером. Задайте вопрос.";

    internal const string Welcome =
        "✨ <b>HabiHamAI</b>\n"
        + "───────────────\n"
        + "• Дневник веса — кнопка ниже 👇\n"
        + "• <b>AI-тренер</b> — кнопка «Спросить тренера» или просто напишите сообщение\n"
        + "• Велозаезд — отправьте файл <b>.tcx</b> в чат (или кнопка «Импорт TCX» / команда /tcx)\n\n"
        + "<i>Совет:</i> команды и справка — в меню <b>☰</b> слева от поля ввода.\n\n"
        + "Если аккаунт ещё не привязан, сделайте это в приложении: профиль → «Подключить Telegram».";

    internal const string Help =
        "<b>Что умеет бот</b>\n"
        + "───────────────\n"
        + "• «Записать вес» или /weight — запись веса в дневник (нужна привязка аккаунта)\n"
        + "• «Спросить тренера» или /trainer — чат с AI-тренером (те же данные, что в приложении)\n"
        + "• Любой текст (при привязанном аккаунте) — сообщение AI-тренеру\n"
        + "• /new — новый диалог с тренером\n"
        + "• Отправка файла <b>.tcx</b> — импорт велотренировки (те же правила, что в приложении)\n"
        + "• /tcx — напоминание, как импортировать велозаезд\n"
        + "• /start — приветствие и кнопки меню\n"
        + "• /keyboard — снова показать кнопки\n"
        + "• /hide — убрать клавиатуру";

    internal const string LinkRequiredForTrainer =
        "Чтобы общаться с AI-тренером, привяжите аккаунт в веб-приложении: профиль → «Подключить Telegram».";
}
