# Telepath

A lightweight middleware pipeline and router for Telegram bots on ASP.NET Core, built on top of [Telegram.Bot](https://github.com/TelegramBots/Telegram.Bot).

- Fluent routing DSL — commands, messages, callbacks, states, regex, custom predicates
- Strongly-typed callback data with zero-reflection serialization
- Stateful conversations via pluggable `IStateService`
- LongPolling and Webhook transports
- Middleware pipeline (custom, error logging, request logging)

## Installation

```
dotnet add package Telepath
```

Requirements: .NET 6, 8, or 10 — `Telegram.Bot 22.x`

## Quick start

```csharp
// Program.cs (Worker Service or ASP.NET Core app)
services.AddTelepath(bot =>
{
    bot.Configure(new BotSettings
    {
        TelegramBotToken = "YOUR_TOKEN",
        Transport = TelepathTransportMode.LongPolling
    });

    bot.UseState(state => state.UseMemoryCache());

    bot.UseRouter(router =>
    {
        router.Command<StartHandler>("start");
        router.Message<NameInputHandler>(BotState.WaitingForName);
        router.Callback<ConfirmCallbackHandler, ConfirmCallbackData>();
    });

    services.AddRouter(typeof(Program).Assembly); // auto-register all handlers
});
```

For Webhook mode, also map the endpoint:

```csharp
app.MapTelepathWebhook("/webhook", secretToken: "YOUR_SECRET");
```

## Routing DSL

All routing methods live on `IRoutingBuilder`. The first matching entry wins.

### Commands

```csharp
router.Command<StartHandler>("start");                        // /start
router.Command<HelpHandler>("help", "h");                    // /help or /h
router.Command<SettingsHandler>(BotState.Admin, "settings"); // /settings when state = Admin
```

### Messages

```csharp
router.Message<AnyTextHandler>();                            // any text message
router.Message<YesHandler>("yes", "да");                     // exact match (case-insensitive)
router.Message<NameInputHandler>(BotState.WaitingForName);   // any message when in state
router.MessageRegex<SearchHandler>(@"^\d{4}$");              // regex
router.Message<CustomHandler>(text => text.StartsWith("#")); // custom predicate
```

### Callbacks

```csharp
router.Callback<ConfirmHandler, ConfirmData>();               // any callback with ConfirmData prefix
router.Callback<ConfirmHandler, ConfirmData>(d => d.Value);  // with predicate
router.Callback<RawHandler>("button_clicked");                // exact callback_data string
router.CallbackRegex<RawHandler>(@"^page:\d+$");             // regex
```

### Update type / other

```csharp
router.WhenUpdateType<PhotoHandler>(UpdateType.Message);
router.WhenMessageType<StickerHandler>(MessageType.Sticker);
router.When<FallbackHandler>(ctx => ctx.Chat?.Type == ChatType.Group);
```

## Handlers

Inherit from `TelepathHandler` and override `HandleAsync()`. Handlers are resolved from DI per request.

```csharp
public class StartHandler : TelepathHandler
{
    protected override async Task HandleAsync()
    {
        await Bot.SendMessage(Context.ChatId!.Value, "Hello! What's your name?");
        Context.State = BotState.WaitingForName;
    }
}

// Register in DI:
services.AddTransient<StartHandler>();
// or auto-register the whole assembly:
services.AddRouter(typeof(Program).Assembly);
```

Convenience base classes:

| Base class | Extras |
|---|---|
| `TelepathHandler` | `Bot`, `Context`, `CancellationToken` |
| `TelepathMessageHandler` | + `Text` property |
| `TelepathCommandHandler` | + `Command`, `Args` properties |
| `TelepathCallbackHandler<TData>` | + auto-deserialized `Data` property |
| `TelepathInlineQueryHandler` | + `InlineQuery` property |

Helper methods available on all handlers:

```csharp
await SendMessageAsync(text, ParseMode.Html, replyMarkup);
await ReplyAsync(text);
await EditMessageTextAsync(text);
await DeleteMessageAsync();
await SendChatActionAsync(ChatAction.Typing);
await AnswerCallbackQueryAsync("Done!", showAlert: false);

// Typed state data (JSON-serialized per key)
SetStateData("user", new UserDto { Name = "Alice" });
var user = GetStateData<UserDto>("user");
```

## Strongly-typed callback data

Define a class, mark it with `[CallbackPrefix]`, declare properties in order:

```csharp
[CallbackPrefix("confirm")]
public class ConfirmData : CallbackData
{
    public bool Confirmed { get; set; }
    public int UserId { get; set; }
}
```

Serialize when building the keyboard, deserialize automatically in the handler:

```csharp
// Building keyboard
var data = new ConfirmData { Confirmed = true, UserId = 42 };
var button = InlineKeyboardButton.WithCallbackData("Yes", data.Serialize());
// serialized: "confirm:True:42"

// Handler
public class ConfirmHandler : TelepathCallbackHandler<ConfirmData>
{
    protected override async Task HandleAsync(ConfirmData data)
    {
        if (data.Confirmed)
            await ReplyAsync($"Confirmed for user {data.UserId}");
    }
}
```

Supported property types: all primitives, `string`, `Guid`, `DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`, `TimeSpan`, enums (stored as int), and their nullable variants. Strings containing `:` are automatically escaped.

## State management

State is scoped per `chatId`. The built-in implementation uses `IMemoryCache` (suitable for development; data is lost on restart).

```csharp
// Configuration
bot.UseState(state => state.UseMemoryCache());

// Custom implementation (Redis, database, etc.)
bot.UseState(state => state.Use<RedisStateService>());
// services.AddSingleton<RedisStateService>();
```

`IStateService` contract:

```csharp
public interface IStateService
{
    Task<(Enum? State, Dictionary<string, string>? Data)> GetAsync(long chatId);
    Task SetAsync(long chatId, Enum? state, Dictionary<string, string>? data);
}
```

State-aware routing matches only when `Context.State` equals the given enum value:

```csharp
router.Message<NameInputHandler>(BotState.WaitingForName);
router.Command<AdminCmdHandler>(AdminState.Active, "ban", "kick");
```

## Custom middleware

```csharp
public class RateLimitMiddleware : ITelepathMiddleware
{
    public async Task InvokeAsync(TelepathContext context, Func<TelepathContext, CancellationToken, Task> next, CancellationToken ct)
    {
        if (!IsAllowed(context.UserId))
            return; // short-circuit

        await next(context, ct);
    }
}

// Register (executed after built-in middleware, before the router)
bot.Use<RateLimitMiddleware>();
// services.AddSingleton<RateLimitMiddleware>();
```

Built-in optional middleware:

```csharp
bot.UseErrorLogging();   // logs unhandled exceptions
bot.UseRequestLogging(); // trace-level update logging
```

## Request flow

```
Telegram Update
    → ITelepathProcessor.HandleUpdate
    → BotInfoMiddleware        (populates Context.BotUser)
    → AnswerCallbackMiddleware (auto-answers unanswered callback queries)
    → StateMiddleware          (loads state; saves changes after pipeline)
    → [your custom middleware]
    → TelepathRouterMiddleware (first matching entry → handler; stops pipeline)
```

## Transport modes

**LongPolling** — uses `ITelegramBotClient.StartReceiving`. No infrastructure needed. Suitable for development and simple deployments.

**Webhook** — registers an ASP.NET Core endpoint. Requires a publicly reachable HTTPS URL. Telepath calls `SetWebhook` on startup with the configured `WebhookUri` and validates the `X-Telegram-Bot-Api-Secret-Token` header using constant-time comparison.

```csharp
// Webhook configuration
bot.Configure(new BotSettings
{
    TelegramBotToken = "TOKEN",
    Transport = TelepathTransportMode.Webhook,
    WebhookUri = "https://yourdomain.com/webhook",
    WebhookSecret = "YOUR_SECRET"
});

// In app setup
app.MapTelepathWebhook("/webhook", secretToken: "YOUR_SECRET");
```

## Running the example

```
cd Telepath.Example
dotnet run
```

Set the `TelegramBotToken` environment variable or update `appsettings.json` before running.

## License

MIT
