using Telepath.Models;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Telepath.Tests;

public class TelepathContextTests
{
    private static User MakeUser(long id = 42, string lang = "ru") =>
        new() { Id = id, FirstName = "Test", IsBot = false, LanguageCode = lang };

    private static Chat MakeChat(long id = 100) =>
        new() { Id = id, Type = ChatType.Private };

    private static Message MakeMessage(User? from = null, Chat? chat = null, string? text = null, int? threadId = null) =>
        new()
        {
            Date = DateTime.UtcNow,
            From = from ?? MakeUser(),
            Chat = chat ?? MakeChat(),
            Text = text,
            MessageThreadId = threadId
        };

    // ── User extraction ────────────────────────────────────────────────────────

    [Fact]
    public void User_ExtractedFrom_MessageUpdate()
    {
        var user = MakeUser(42);
        var ctx = new TelepathContext { Update = new Update { Id = 1, Message = MakeMessage(from: user) } };
        Assert.Equal(42, ctx.UserId);
    }

    [Fact]
    public void User_ExtractedFrom_CallbackQueryUpdate()
    {
        var user = MakeUser(99);
        var ctx = new TelepathContext
        {
            Update = new Update
            {
                Id = 1,
                CallbackQuery = new CallbackQuery { Id = "1", From = user, Data = "d" }
            }
        };
        Assert.Equal(99, ctx.UserId);
    }

    [Fact]
    public void User_ExtractedFrom_EditedMessageUpdate()
    {
        var user = MakeUser(55);
        var ctx = new TelepathContext { Update = new Update { Id = 1, EditedMessage = MakeMessage(from: user) } };
        Assert.Equal(55, ctx.UserId);
    }

    [Fact]
    public void User_IsNull_ForUnknownUpdateType()
    {
        var ctx = new TelepathContext { Update = new Update { Id = 1 } };
        Assert.Null(ctx.User);
        Assert.Null(ctx.UserId);
    }

    // ── Chat extraction ────────────────────────────────────────────────────────

    [Fact]
    public void Chat_ExtractedFrom_MessageUpdate()
    {
        var chat = MakeChat(200);
        var ctx = new TelepathContext { Update = new Update { Id = 1, Message = MakeMessage(chat: chat) } };
        Assert.Equal(200, ctx.ChatId);
    }

    [Fact]
    public void Chat_ExtractedFrom_EditedMessageUpdate()
    {
        var chat = MakeChat(300);
        var ctx = new TelepathContext { Update = new Update { Id = 1, EditedMessage = MakeMessage(chat: chat) } };
        Assert.Equal(300, ctx.ChatId);
    }

    [Fact]
    public void Chat_ExtractedFrom_ChannelPostUpdate()
    {
        var chat = MakeChat(400);
        var ctx = new TelepathContext { Update = new Update { Id = 1, ChannelPost = MakeMessage(chat: chat) } };
        Assert.Equal(400, ctx.ChatId);
    }

    [Fact]
    public void Chat_IsNull_ForInlineQueryUpdate()
    {
        var ctx = new TelepathContext
        {
            Update = new Update
            {
                Id = 1,
                InlineQuery = new InlineQuery { Id = "1", From = MakeUser(), Query = "q", Offset = "0" }
            }
        };
        Assert.Null(ctx.Chat);
        Assert.Null(ctx.ChatId);
    }

    // ── MessageId extraction ───────────────────────────────────────────────────

    [Fact]
    public void MessageId_ExtractedFrom_MessageUpdate()
    {
        var ctx = new TelepathContext { Update = new Update { Id = 1, Message = MakeMessage() } };
        Assert.NotNull(ctx.MessageId);
    }

    [Fact]
    public void MessageId_ExtractedFrom_CallbackQueryUpdate()
    {
        var ctx = new TelepathContext
        {
            Update = new Update
            {
                Id = 1,
                CallbackQuery = new CallbackQuery { Id = "1", From = MakeUser(), Data = "d", Message = MakeMessage() }
            }
        };
        Assert.NotNull(ctx.MessageId);
    }

    [Fact]
    public void MessageId_IsNull_ForUnknownUpdateType()
    {
        var ctx = new TelepathContext { Update = new Update { Id = 1 } };
        Assert.Null(ctx.MessageId);
    }

    // ── ThreadId extraction ────────────────────────────────────────────────────

    [Fact]
    public void ThreadId_ExtractedFrom_MessageUpdate()
    {
        var ctx = new TelepathContext { Update = new Update { Id = 1, Message = MakeMessage(threadId: 5) } };
        Assert.Equal(5, ctx.ThreadId);
    }

    [Fact]
    public void ThreadId_IsNull_WhenMessageHasNoThread()
    {
        var ctx = new TelepathContext { Update = new Update { Id = 1, Message = MakeMessage() } };
        Assert.Null(ctx.ThreadId);
    }

    // ── LanguageCode ───────────────────────────────────────────────────────────

    [Fact]
    public void LanguageCode_ReturnsUserLanguageCode()
    {
        var user = MakeUser(lang: "de");
        var ctx = new TelepathContext { Update = new Update { Id = 1, Message = MakeMessage(from: user) } };
        Assert.Equal("de", ctx.LanguageCode);
    }

    [Fact]
    public void LanguageCode_FallsBackToEn_WhenLanguageCodeIsNull()
    {
        var user = new User { Id = 1, FirstName = "Test", IsBot = false, LanguageCode = null };
        var ctx = new TelepathContext { Update = new Update { Id = 1, Message = MakeMessage(from: user) } };
        Assert.Equal("en", ctx.LanguageCode);
    }

    [Fact]
    public void LanguageCode_FallsBackToEn_WhenNoUser()
    {
        var ctx = new TelepathContext { Update = new Update { Id = 1 } };
        Assert.Equal("en", ctx.LanguageCode);
    }
}
