using System.Text.Json;
using Telepath.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Telepath.Routing.Handlers;

public abstract class TelepathHandler : IRouterHandler
{
    protected ITelegramBotClient Bot { get; private set; } = null!;
    protected TelepathContext Context { get; private set; } = null!;
    protected CancellationToken CancellationToken { get; private set; }

    Task IRouterHandler.ExecuteAsync(TelepathContext context, ITelegramBotClient bot, CancellationToken cancellationToken)
    {
        Context = context;
        Bot = bot;
        CancellationToken = cancellationToken;
        return HandleAsync();
    }

    protected abstract Task HandleAsync();

    protected Task<Message> SendMessageAsync(
        string text,
        ParseMode parseMode = ParseMode.None,
        ReplyMarkup? replyMarkup = null)
    {
        return Bot.SendMessage(
            Context.ChatId!.Value,
            text,
            parseMode: parseMode,
            replyMarkup: replyMarkup,
            cancellationToken: CancellationToken);
    }

    protected Task<Message> ReplyAsync(
        string text,
        ParseMode parseMode = ParseMode.None,
        ReplyMarkup? replyMarkup = null)
    {
        return Bot.SendMessage(
            Context.ChatId!.Value,
            text,
            parseMode: parseMode,
            replyMarkup: replyMarkup,
            replyParameters: new ReplyParameters { MessageId = Context.MessageId!.Value },
            cancellationToken: CancellationToken);
    }

    protected Task<Message> EditMessageTextAsync(
        string text,
        ParseMode parseMode = ParseMode.None,
        InlineKeyboardMarkup? replyMarkup = null)
    {
        return Bot.EditMessageText(
            Context.ChatId!.Value,
            Context.MessageId!.Value,
            text,
            parseMode: parseMode,
            replyMarkup: replyMarkup,
            cancellationToken: CancellationToken);
    }

    protected Task DeleteMessageAsync()
    {
        return Bot.DeleteMessage(
            Context.ChatId!.Value,
            Context.MessageId!.Value,
            CancellationToken);
    }

    protected Task SendChatActionAsync(ChatAction action)
    {
        return Bot.SendChatAction(
            Context.ChatId!.Value,
            action,
            cancellationToken: CancellationToken);
    }

    protected T? GetStateData<T>(string key)
    {
        if (Context.StateData == null || !Context.StateData.TryGetValue(key, out var value))
            return default;
        return JsonSerializer.Deserialize<T>(value);
    }

    protected void SetStateData<T>(string key, T value)
    {
        Context.StateData ??= new Dictionary<string, string>();
        Context.StateData[key] = JsonSerializer.Serialize(value);
    }

    protected Task AnswerCallbackQueryAsync(
        string? text = null,
        bool showAlert = false,
        string? url = null,
        int cacheTime = 0)
    {
        Context.CallbackQueryAnswered = true;
        return Bot.AnswerCallbackQuery(
            Context.Update.CallbackQuery!.Id,
            text,
            showAlert,
            url,
            cacheTime,
            CancellationToken);
    }
}
