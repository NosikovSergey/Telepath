using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Telepath.Models;

public class TelepathContext
{
    public Update Update { get; set; } = null!;
    public Enum? State { get; set; }
    public Dictionary<string, string>? StateData { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
    public User? BotUser { get; internal set; }

    public User? User => GetUser(Update);
    public Chat? Chat => GetChat(Update);
    public long? ChatId => Chat?.Id;
    public long? UserId => User?.Id;
    public int? MessageId => GetMessageId(Update);
    public int? ThreadId => GetThreadId(Update);
    public string LanguageCode => User?.LanguageCode ?? "en";
    public bool CallbackQueryAnswered { get; set; }

    private static User? GetUser(Update update)
    {
        return update.Type switch
        {
            UpdateType.Message => update.Message!.From,
            UpdateType.EditedMessage => update.EditedMessage!.From,
            UpdateType.CallbackQuery => update.CallbackQuery!.From,
            UpdateType.InlineQuery => update.InlineQuery!.From,
            UpdateType.ChosenInlineResult => update.ChosenInlineResult!.From,
            UpdateType.ShippingQuery => update.ShippingQuery!.From,
            UpdateType.PreCheckoutQuery => update.PreCheckoutQuery!.From,
            UpdateType.PollAnswer => update.PollAnswer!.User,
            UpdateType.MyChatMember => update.MyChatMember!.From,
            UpdateType.ChatMember => update.ChatMember!.From,
            UpdateType.ChatJoinRequest => update.ChatJoinRequest!.From,
            _ => null
        };
    }

    private static Chat? GetChat(Update update)
    {
        return update.Type switch
        {
            UpdateType.Message => update.Message!.Chat,
            UpdateType.EditedMessage => update.EditedMessage!.Chat,
            UpdateType.ChannelPost => update.ChannelPost!.Chat,
            UpdateType.EditedChannelPost => update.EditedChannelPost!.Chat,
            UpdateType.CallbackQuery => update.CallbackQuery!.Message?.Chat,
            UpdateType.MyChatMember => update.MyChatMember!.Chat,
            UpdateType.ChatMember => update.ChatMember!.Chat,
            UpdateType.ChatJoinRequest => update.ChatJoinRequest!.Chat,
            _ => null
        };
    }

    private static int? GetMessageId(Update update)
    {
        return update.Type switch
        {
            UpdateType.Message => update.Message!.MessageId,
            UpdateType.EditedMessage => update.EditedMessage!.MessageId,
            UpdateType.ChannelPost => update.ChannelPost!.MessageId,
            UpdateType.EditedChannelPost => update.EditedChannelPost!.MessageId,
            UpdateType.CallbackQuery => update.CallbackQuery!.Message?.MessageId,
            _ => null
        };
    }

    private static int? GetThreadId(Update update)
    {
        return update.Type switch
        {
            UpdateType.Message => update.Message!.MessageThreadId,
            UpdateType.EditedMessage => update.EditedMessage!.MessageThreadId,
            UpdateType.ChannelPost => update.ChannelPost!.MessageThreadId,
            UpdateType.EditedChannelPost => update.EditedChannelPost!.MessageThreadId,
            UpdateType.CallbackQuery => update.CallbackQuery!.Message?.MessageThreadId,
            _ => null
        };
    }
}
