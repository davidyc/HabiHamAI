using System.Globalization;
using System.Text.Json;
using HabiHamAIAPI.Data;
using HabiHamAIAPI.Models;
using HabiHamAIAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace HabiHamAIAPI.Services.Telegram;

public sealed class TelegramUpdateHandler : ITelegramUpdateHandler
{
    private const decimal MinWeightKg = 0.01m;
    private const decimal MaxWeightKg = 700m;
    private const long MaxTelegramBotFileBytes = 20L * 1024 * 1024;

    private readonly ITelegramBotClient _botClient;
    private readonly TelegramChatStateStore _state;
    private readonly AppDbContext _dbContext;
    private readonly ITelegramUserLinkService _linkService;
    private readonly IUserWeightRecordingService _weightRecording;
    private readonly IBikeActivitiesService _bikeActivities;
    private readonly ILogger<TelegramUpdateHandler> _logger;

    public TelegramUpdateHandler(
        ITelegramBotClient botClient,
        TelegramChatStateStore state,
        AppDbContext dbContext,
        ITelegramUserLinkService linkService,
        IUserWeightRecordingService weightRecording,
        IBikeActivitiesService bikeActivities,
        ILogger<TelegramUpdateHandler> logger)
    {
        _botClient = botClient;
        _state = state;
        _dbContext = dbContext;
        _linkService = linkService;
        _weightRecording = weightRecording;
        _bikeActivities = bikeActivities;
        _logger = logger;
    }

    public async Task HandleAsync(Update update, CancellationToken cancellationToken)
    {
        if (update.Type != UpdateType.Message || update.Message is not { } message)
        {
            return;
        }

        var chatId = message.Chat.Id;
        if (message.Chat.Type != ChatType.Private)
        {
            await _botClient.SendMessage(
                chatId,
                "Бот работает только в личном чате. Откройте диалог с ботом напрямую.",
                cancellationToken: cancellationToken);
            return;
        }

        if (message.Document is { } document)
        {
            await HandleDocumentAsync(chatId, document, cancellationToken);
            return;
        }

        var text = message.Text;
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        if (TelegramStartPayloadParser.TryParse(text, out var startPayload))
        {
            if (!string.IsNullOrEmpty(startPayload))
            {
                var linkResult = await _linkService.TryConsumeStartPayloadAsync(startPayload, chatId, cancellationToken);
                var linkMsg = linkResult.Status switch
                {
                    TelegramLinkConsumeStatus.Linked =>
                        "Telegram подключён к вашему аккаунту. Можно записывать вес и присылать файлы .tcx для велотренировок.",
                    TelegramLinkConsumeStatus.AlreadyLinkedSameChat =>
                        "Этот Telegram уже был подключён к вашему аккаунту.",
                    TelegramLinkConsumeStatus.InvalidOrExpiredToken =>
                        "Ссылка недействительна или устарела. Создайте новую в приложении (профиль → Telegram).",
                    TelegramLinkConsumeStatus.ChatBelongsToOtherUser =>
                        "Этот Telegram уже привязан к другому аккаунту.",
                    _ => "Не удалось подключить Telegram."
                };
                await _botClient.SendMessage(
                    chatId,
                    linkMsg,
                    replyMarkup: TelegramBotMenu.MainKeyboard,
                    cancellationToken: cancellationToken);
                return;
            }
        }

        var command = ParseBotCommand(text);

        switch (command)
        {
            case "start":
                _state.Set(chatId, TelegramChatDialogState.Idle);
                await _botClient.SendMessage(
                    chatId,
                    TelegramBotMenu.Welcome,
                    parseMode: ParseMode.Html,
                    replyMarkup: TelegramBotMenu.MainKeyboard,
                    cancellationToken: cancellationToken);
                return;
            case "weight":
                await BeginWeightInputAsync(chatId, cancellationToken);
                return;
            case "tcx":
                await _botClient.SendMessage(
                    chatId,
                    TelegramBotMenu.ImportTcxHint,
                    parseMode: ParseMode.Html,
                    replyMarkup: TelegramBotMenu.MainKeyboard,
                    cancellationToken: cancellationToken);
                return;
            case "help":
                await _botClient.SendMessage(
                    chatId,
                    TelegramBotMenu.Help,
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken);
                return;
            case "keyboard":
                await _botClient.SendMessage(
                    chatId,
                    "Кнопки меню снова на экране.",
                    replyMarkup: TelegramBotMenu.MainKeyboard,
                    cancellationToken: cancellationToken);
                return;
            case "hide":
                _state.Set(chatId, TelegramChatDialogState.Idle);
                await _botClient.SendMessage(
                    chatId,
                    "Клавиатура скрыта. Вернуть: /keyboard или команда в меню ☰.",
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken);
                return;
        }

        if (text == TelegramBotMenu.BtnSendWeight)
        {
            await BeginWeightInputAsync(chatId, cancellationToken);
            return;
        }

        if (text == TelegramBotMenu.BtnImportTcx)
        {
            await _botClient.SendMessage(
                chatId,
                TelegramBotMenu.ImportTcxHint,
                parseMode: ParseMode.Html,
                replyMarkup: TelegramBotMenu.MainKeyboard,
                cancellationToken: cancellationToken);
            return;
        }

        if (text == TelegramBotMenu.BtnCancelWeight)
        {
            _state.Set(chatId, TelegramChatDialogState.Idle);
            await _botClient.SendMessage(
                chatId,
                "Ввод веса отменён.",
                replyMarkup: TelegramBotMenu.MainKeyboard,
                cancellationToken: cancellationToken);
            return;
        }

        if (_state.Get(chatId) == TelegramChatDialogState.AwaitingWeightKg)
        {
            if (TryParseWeightKg(text, out var kg))
            {
                if (kg < MinWeightKg || kg > MaxWeightKg)
                {
                    await _botClient.SendMessage(
                        chatId,
                        $"Число должно быть от {MinWeightKg.ToString(CultureInfo.InvariantCulture)} до {MaxWeightKg.ToString(CultureInfo.InvariantCulture)} кг. Попробуйте ещё раз.",
                        cancellationToken: cancellationToken);
                    return;
                }

                var appUserId = await _dbContext.Users
                    .AsNoTracking()
                    .Where(u => u.TelegramChatId == chatId)
                    .Select(u => (Guid?)u.Id)
                    .FirstOrDefaultAsync(cancellationToken);
                if (appUserId is null)
                {
                    _state.Set(chatId, TelegramChatDialogState.Idle);
                    await _botClient.SendMessage(
                        chatId,
                        "Сначала привяжите аккаунт в веб-приложении: профиль → «Подключить Telegram».",
                        replyMarkup: TelegramBotMenu.MainKeyboard,
                        cancellationToken: cancellationToken);
                    return;
                }

                try
                {
                    await _weightRecording.RecordWeightTrackerEntryAsync(
                        appUserId.Value,
                        DateOnly.FromDateTime(DateTime.UtcNow),
                        kg,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Telegram: failed to save weight for chat {ChatId}", chatId);
                    await _botClient.SendMessage(
                        chatId,
                        "Не удалось сохранить вес. Попробуйте позже или введите вес в приложении.",
                        cancellationToken: cancellationToken);
                    return;
                }

                _state.Set(chatId, TelegramChatDialogState.Idle);
                await _botClient.SendMessage(
                    chatId,
                    $"Вес сохранён в дневнике: <b>{kg.ToString(CultureInfo.InvariantCulture)}</b> кг.",
                    parseMode: ParseMode.Html,
                    replyMarkup: TelegramBotMenu.MainKeyboard,
                    cancellationToken: cancellationToken);
                return;
            }

            await _botClient.SendMessage(
                chatId,
                "Сейчас ожидается только число веса в кг (например 72.5). Либо нажмите «↩️  Отмена».",
                cancellationToken: cancellationToken);
            return;
        }

        await _botClient.SendMessage(
            chatId,
            $"Вы написали: {text}",
            cancellationToken: cancellationToken);
    }

    private async Task HandleDocumentAsync(long chatId, Document document, CancellationToken cancellationToken)
    {
        if (_state.Get(chatId) == TelegramChatDialogState.AwaitingWeightKg)
        {
            await _botClient.SendMessage(
                chatId,
                "Сейчас идёт ввод веса. Введите число в кг или нажмите «Отмена», затем отправьте файл .tcx.",
                replyMarkup: TelegramBotMenu.WeightInputKeyboard,
                cancellationToken: cancellationToken);
            return;
        }

        var ext = Path.GetExtension(document.FileName ?? string.Empty);
        if (!string.Equals(ext, ".tcx", StringComparison.OrdinalIgnoreCase))
        {
            await _botClient.SendMessage(
                chatId,
                "Для загрузки через бота принимаются только файлы <b>.tcx</b> (велотренировка). Остальные типы файлов пропускаются.",
                parseMode: ParseMode.Html,
                replyMarkup: TelegramBotMenu.MainKeyboard,
                cancellationToken: cancellationToken);
            return;
        }

        if (document.FileSize is long size && size > MaxTelegramBotFileBytes)
        {
            await _botClient.SendMessage(
                chatId,
                "Файл слишком большой для бота (лимит Telegram — 20 МБ). Импортируйте через веб-приложение.",
                replyMarkup: TelegramBotMenu.MainKeyboard,
                cancellationToken: cancellationToken);
            return;
        }

        var appUserId = await _dbContext.Users
            .AsNoTracking()
            .Where(u => u.TelegramChatId == chatId)
            .Select(u => (Guid?)u.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (appUserId is null)
        {
            await _botClient.SendMessage(
                chatId,
                "Сначала привяжите аккаунт в веб-приложении: профиль → «Подключить Telegram».",
                replyMarkup: TelegramBotMenu.MainKeyboard,
                cancellationToken: cancellationToken);
            return;
        }

        await using var fileStream = new MemoryStream();
        try
        {
            await _botClient.GetInfoAndDownloadFile(document.FileId, fileStream, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Telegram: failed to download document for chat {ChatId}", chatId);
            await _botClient.SendMessage(
                chatId,
                "Не удалось скачать файл из Telegram. Попробуйте ещё раз или загрузите через приложение.",
                replyMarkup: TelegramBotMenu.MainKeyboard,
                cancellationToken: cancellationToken);
            return;
        }

        fileStream.Position = 0;
        IActionResult importResult;
        try
        {
            importResult = await _bikeActivities.ImportTcxForUserIdAsync(
                appUserId.Value,
                fileStream,
                document.FileName,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Telegram: TCX import failed for chat {ChatId}", chatId);
            await _botClient.SendMessage(
                chatId,
                "Ошибка при импорте. Попробуйте позже или загрузите файл через веб-приложение.",
                replyMarkup: TelegramBotMenu.MainKeyboard,
                cancellationToken: cancellationToken);
            return;
        }

        if (importResult is OkObjectResult { Value: BikeActivitySummaryResponse summary })
        {
            var distancePart = summary.DistanceMeters is > 0
                ? $", дистанция <b>{(summary.DistanceMeters.Value / 1000.0).ToString("0.##", CultureInfo.InvariantCulture)}</b> км"
                : string.Empty;
            var pointsPart = summary.TrackpointCount > 0
                ? $", точек трека: <b>{summary.TrackpointCount}</b>"
                : string.Empty;
            await _botClient.SendMessage(
                chatId,
                "Велозаезд импортирован в ваш аккаунт.\n"
                    + $"Начало (UTC): <b>{summary.StartTimeUtc:yyyy-MM-dd HH:mm}</b>{distancePart}{pointsPart}.\n"
                    + "Подробности — в приложении, раздел «Велотренировки».",
                parseMode: ParseMode.Html,
                replyMarkup: TelegramBotMenu.MainKeyboard,
                cancellationToken: cancellationToken);
            return;
        }

        var err = TryGetJsonMessageFromActionResult(importResult) ?? "Не удалось импортировать файл.";
        await _botClient.SendMessage(
            chatId,
            err,
            replyMarkup: TelegramBotMenu.MainKeyboard,
            cancellationToken: cancellationToken);
    }

    private static string? TryGetJsonMessageFromActionResult(IActionResult result)
    {
        if (result is not ObjectResult { Value: { } value })
        {
            return null;
        }

        try
        {
            var json = JsonSerializer.Serialize(value);
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("message", out var m) ? m.GetString() : null;
        }
        catch
        {
            return null;
        }
    }

    private async Task BeginWeightInputAsync(long chatId, CancellationToken cancellationToken)
    {
        var linked = await _dbContext.Users.AsNoTracking().AnyAsync(u => u.TelegramChatId == chatId, cancellationToken);
        if (!linked)
        {
            await _botClient.SendMessage(
                chatId,
                "Сначала привяжите аккаунт в веб-приложении: профиль → «Подключить Telegram», затем откройте ссылку в этом чате.",
                replyMarkup: TelegramBotMenu.MainKeyboard,
                cancellationToken: cancellationToken);
            return;
        }

        _state.Set(chatId, TelegramChatDialogState.AwaitingWeightKg);
        await _botClient.SendMessage(
            chatId,
            TelegramBotMenu.WeightPrompt,
            parseMode: ParseMode.Html,
            replyMarkup: TelegramBotMenu.WeightInputKeyboard,
            cancellationToken: cancellationToken);
    }

    /// <summary>Сообщение целиком должно быть одним десятичным числом (запятая или точка как разделитель).</summary>
    private static bool TryParseWeightKg(string text, out decimal kg)
    {
        kg = default;
        var s = text.Trim().Replace(",", ".", StringComparison.Ordinal);
        if (s.Length == 0)
        {
            return false;
        }

        return decimal.TryParse(
            s,
            NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture,
            out kg);
    }

    /// <summary>Извлекает имя команды без / и без @botname.</summary>
    private static string? ParseBotCommand(string text)
    {
        if (!text.StartsWith('/'))
        {
            return null;
        }

        var rest = text.AsSpan(1);
        var space = rest.IndexOf(' ');
        if (space >= 0)
        {
            rest = rest[..space];
        }

        var at = rest.IndexOf('@');
        var name = at >= 0 ? rest[..at] : rest;
        return name.Length == 0 ? null : name.ToString().ToLowerInvariant();
    }
}
