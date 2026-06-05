using Telepath.Models;
using Telepath.Routing;
using Telepath.Routing.Handlers;
using Telepath.Routing.Internal;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Telepath.Tests;

public class RouterFlowBuilderExtensionsTests
{
    private enum TestState { Active, Waiting }

    private class FakeHandler : IRouterHandler
    {
        public Task ExecuteAsync(TelepathContext context, ITelegramBotClient bot, CancellationToken ct) => Task.CompletedTask;
    }

    private static TelepathContext MessageContext(string? text, Enum? state = null) => new()
    {
        Update = new Update
        {
            Id = 1,
            Message = new Message
            {
                Date = DateTime.UtcNow,
                Chat = new Chat { Id = 1, Type = ChatType.Private },
                Text = text
            }
        },
        State = state
    };

    private static TelepathContext CallbackContext(string data) => new()
    {
        Update = new Update
        {
            Id = 1,
            CallbackQuery = new CallbackQuery
            {
                Id = "1",
                From = new User { Id = 1, FirstName = "Test", IsBot = false },
                Data = data
            }
        }
    };

    private static IRoute SingleEntry(Action<IRoutingBuilder> configure)
    {
        var builder = new RoutingBuilder();
        configure(builder);
        return builder.Build().Single();
    }

    // ── Command routing ────────────────────────────────────────────────────────

    [Fact]
    public void Command_Matches_SimpleCommand()
    {
        var entry = SingleEntry(b => b.Command<FakeHandler>("start"));
        Assert.True(entry.Matches(MessageContext("/start")));
    }

    [Fact]
    public void Command_Matches_CaseInsensitive()
    {
        var entry = SingleEntry(b => b.Command<FakeHandler>("start"));
        Assert.True(entry.Matches(MessageContext("/START")));
    }

    [Fact]
    public void Command_Matches_CommandWithArguments()
    {
        var entry = SingleEntry(b => b.Command<FakeHandler>("start"));
        Assert.True(entry.Matches(MessageContext("/start param1 param2")));
    }

    [Fact]
    public void Command_Matches_OneOfMultipleRegisteredCommands()
    {
        var entry = SingleEntry(b => b.Command<FakeHandler>("start", "help"));
        Assert.True(entry.Matches(MessageContext("/help")));
    }

    [Fact]
    public void Command_DoesNotMatch_DifferentCommand()
    {
        var entry = SingleEntry(b => b.Command<FakeHandler>("start"));
        Assert.False(entry.Matches(MessageContext("/help")));
    }

    [Fact]
    public void Command_DoesNotMatch_PlainText()
    {
        var entry = SingleEntry(b => b.Command<FakeHandler>("start"));
        Assert.False(entry.Matches(MessageContext("hello")));
    }

    [Fact]
    public void Command_DoesNotMatch_EmptyCommand()
    {
        var entry = SingleEntry(b => b.Command<FakeHandler>("start"));
        Assert.False(entry.Matches(MessageContext("/")));
    }

    [Fact]
    public void Command_DoesNotMatch_NullText()
    {
        var entry = SingleEntry(b => b.Command<FakeHandler>("start"));
        Assert.False(entry.Matches(MessageContext(null)));
    }

    [Fact]
    public void Command_DoesNotMatch_NonMessageUpdate()
    {
        var entry = SingleEntry(b => b.Command<FakeHandler>("start"));
        Assert.False(entry.Matches(CallbackContext("/start")));
    }

    [Fact]
    public void Command_WithMention_Matches_WhenMentionMatchesBotUsername()
    {
        var entry = SingleEntry(b => b.Command<FakeHandler>("start"));
        var ctx = MessageContext("/start@mybot");
        ctx.BotUser = new User { Id = 1, FirstName = "Bot", IsBot = true, Username = "mybot" };
        Assert.True(entry.Matches(ctx));
    }

    [Fact]
    public void Command_WithMention_Matches_CaseInsensitiveBotUsername()
    {
        var entry = SingleEntry(b => b.Command<FakeHandler>("start"));
        var ctx = MessageContext("/start@MYBOT");
        ctx.BotUser = new User { Id = 1, FirstName = "Bot", IsBot = true, Username = "mybot" };
        Assert.True(entry.Matches(ctx));
    }

    [Fact]
    public void Command_WithMention_DoesNotMatch_DifferentBotUsername()
    {
        var entry = SingleEntry(b => b.Command<FakeHandler>("start"));
        var ctx = MessageContext("/start@otherbot");
        ctx.BotUser = new User { Id = 1, FirstName = "Bot", IsBot = true, Username = "mybot" };
        Assert.False(entry.Matches(ctx));
    }

    [Fact]
    public void Command_WithMention_Matches_WhenBotUserIsUnknown()
    {
        // When bot doesn't know its own username yet, mention filtering is skipped
        var entry = SingleEntry(b => b.Command<FakeHandler>("start"));
        var ctx = MessageContext("/start@otherbot");
        ctx.BotUser = null;
        Assert.True(entry.Matches(ctx));
    }

    // ── State-scoped command routing ───────────────────────────────────────────

    [Fact]
    public void Command_WithState_Matches_WhenStateMatches()
    {
        var entry = SingleEntry(b => b.Command<FakeHandler>(TestState.Active, "start"));
        Assert.True(entry.Matches(MessageContext("/start", TestState.Active)));
    }

    [Fact]
    public void Command_WithState_DoesNotMatch_WhenStateDiffers()
    {
        var entry = SingleEntry(b => b.Command<FakeHandler>(TestState.Active, "start"));
        Assert.False(entry.Matches(MessageContext("/start", TestState.Waiting)));
    }

    [Fact]
    public void Command_WithState_DoesNotMatch_WhenStateIsNull()
    {
        var entry = SingleEntry(b => b.Command<FakeHandler>(TestState.Active, "start"));
        Assert.False(entry.Matches(MessageContext("/start")));
    }

    // ── Message routing ────────────────────────────────────────────────────────

    [Fact]
    public void Message_Matches_AnyTextMessage()
    {
        var entry = SingleEntry(b => b.Message<FakeHandler>());
        Assert.True(entry.Matches(MessageContext("anything")));
    }

    [Fact]
    public void Message_DoesNotMatch_NonTextUpdate()
    {
        var entry = SingleEntry(b => b.Message<FakeHandler>());
        Assert.False(entry.Matches(CallbackContext("data")));
    }

    [Fact]
    public void Message_DoesNotMatch_NullText()
    {
        var entry = SingleEntry(b => b.Message<FakeHandler>());
        Assert.False(entry.Matches(MessageContext(null)));
    }

    [Fact]
    public void Message_WithValues_Matches_ExactText()
    {
        var entry = SingleEntry(b => b.Message<FakeHandler>("hello", "hi"));
        Assert.True(entry.Matches(MessageContext("hello")));
    }

    [Fact]
    public void Message_WithValues_Matches_CaseInsensitive()
    {
        var entry = SingleEntry(b => b.Message<FakeHandler>("hello"));
        Assert.True(entry.Matches(MessageContext("HELLO")));
    }

    [Fact]
    public void Message_WithValues_DoesNotMatch_DifferentText()
    {
        var entry = SingleEntry(b => b.Message<FakeHandler>("hello"));
        Assert.False(entry.Matches(MessageContext("bye")));
    }

    [Fact]
    public void Message_WithState_Matches_WhenStateAndTextMatch()
    {
        var entry = SingleEntry(b => b.Message<FakeHandler>(TestState.Waiting));
        Assert.True(entry.Matches(MessageContext("any text", TestState.Waiting)));
    }

    [Fact]
    public void Message_WithState_DoesNotMatch_WhenStateDiffers()
    {
        var entry = SingleEntry(b => b.Message<FakeHandler>(TestState.Waiting));
        Assert.False(entry.Matches(MessageContext("any text", TestState.Active)));
    }

    [Fact]
    public void MessageRegex_Matches_WhenPatternMatches()
    {
        var entry = SingleEntry(b => b.MessageRegex<FakeHandler>(@"^\d{3}-\d{4}$"));
        Assert.True(entry.Matches(MessageContext("123-4567")));
    }

    [Fact]
    public void MessageRegex_DoesNotMatch_WhenPatternFails()
    {
        var entry = SingleEntry(b => b.MessageRegex<FakeHandler>(@"^\d{3}-\d{4}$"));
        Assert.False(entry.Matches(MessageContext("hello")));
    }

    // ── Callback routing ───────────────────────────────────────────────────────

    [Fact]
    public void Callback_Matches_AnyCallbackQuery()
    {
        var entry = SingleEntry(b => b.Callback<FakeHandler>());
        Assert.True(entry.Matches(CallbackContext("anything")));
    }

    [Fact]
    public void Callback_DoesNotMatch_MessageUpdate()
    {
        var entry = SingleEntry(b => b.Callback<FakeHandler>());
        Assert.False(entry.Matches(MessageContext("text")));
    }

    [Fact]
    public void Callback_WithValues_Matches_ExactData()
    {
        var entry = SingleEntry(b => b.Callback<FakeHandler>("confirm", "cancel"));
        Assert.True(entry.Matches(CallbackContext("confirm")));
    }

    [Fact]
    public void Callback_WithValues_DoesNotMatch_DifferentData()
    {
        var entry = SingleEntry(b => b.Callback<FakeHandler>("confirm"));
        Assert.False(entry.Matches(CallbackContext("cancel")));
    }

    [Fact]
    public void CallbackRegex_Matches_WhenPatternMatches()
    {
        var entry = SingleEntry(b => b.CallbackRegex<FakeHandler>(@"^confirm:\d+$"));
        Assert.True(entry.Matches(CallbackContext("confirm:42")));
    }

    [Fact]
    public void CallbackRegex_DoesNotMatch_WhenPatternFails()
    {
        var entry = SingleEntry(b => b.CallbackRegex<FakeHandler>(@"^confirm:\d+$"));
        Assert.False(entry.Matches(CallbackContext("cancel:42")));
    }
}
