# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Solution Structure

Three projects targeting `net8.0`:

- `Telepath` — the library (NuGet package). References `Microsoft.AspNetCore.App` and `Telegram.Bot 22.5.1`. `InternalsVisibleTo` grants test access.
- `Telepath.Example` — Worker Service demonstrating LongPolling setup.
- `Telepath.Tests` — xUnit tests (no mocking framework; tests are direct unit tests of library internals).

## Commands

```
# Build
dotnet build

# Run all tests
dotnet test

# Run a single test class
dotnet test --filter "FullyQualifiedName~CallbackDataTests"

# Run a single test method
dotnet test --filter "FullyQualifiedName~CallbackDataTests.Pack_WithFields_ReturnsCorrectString"

# Pack NuGet package
dotnet pack Telepath/Telepath.csproj -c Release
```

## Architecture

### Request flow

Telegram `Update` → `ITelepathProcessor.HandleUpdate` → `MiddlewarePipeline.InvokeAsync` → ordered middlewares → `TelepathRouterMiddleware` → matching `IRouterHandler`.

The `MiddlewarePipeline` is built once at startup as a `Singleton`. It always prepends two core middlewares before user-registered ones:
1. `BotInfoMiddleware` — populates `TelepathContext.Me` (bot's own `User`).
2. `AnswerCallbackQueryMiddleware` — auto-answers unanswered callback queries after the pipeline runs.
3. `StateMiddleware` — optional; reads/writes per-chat state via `IStateService` around each request.

### Configuration entry point

`AddTelepath(IServiceCollection, Action<ITelepathConfigurator>)` in `TelepathConfigurationExtensions`. The configurator collects settings, middleware factories, and state config, then registers everything in `TelepathConfigurator.Configure`.

`BotSettings.Mode` selects the transport: `LongPolling` registers `LongPollingInitializer` (hosted service using `StartReceiving`), `Webhook` registers `WebhookInitializer` (calls `SetWebhook` on startup) and requires `MapTelepathWebhook` on the `IEndpointRouteBuilder`.

### Routing

`TelepathRouterMiddleware` is registered via `UseRouter(IRouterFlowBuilder)`. The builder holds an ordered `List<IRouterEntry>`, each pairing a predicate with a handler type. The first matching entry wins.

`RouterFlowBuilderExtensions` provides the routing DSL: `Command<T>`, `Message<T>`, `Callback<T>`, `CallbackRoute<THandler, TCallbackData>`, `State<T>`, `When<T>`, `WhenType<T>`, etc.

Handlers inherit `TelepathHandler` (override `HandleAsync()`) or `TelepathHandler<TCallbackData>` (override `HandleAsync(TCallbackData)` — auto-unpacks callback data). Handler instances are resolved from DI per-request; they must be registered with `AddTransient<MyHandler>()` or via `AddRouter(assembly)` which scans for all `IRouterHandler` implementations.

### CallbackData serialization

`CallbackData` subclasses carry typed callback query payloads. They must have `[CallbackPrefix("prefix")]`. Serialization format: `prefix:field1:field2:...` where fields are ordered by declaration (`MetadataToken`). Colons in string values are backslash-escaped. Supports all primitive types, `Guid`, date/time types, enums (stored as integer), and nullable variants.

`CallbackDataSerializer` uses compiled `Expression` lambdas for getters/setters (cached per type) to avoid reflection overhead at runtime.

### State management

`IStateService` stores `(Enum? state, Dictionary<string, string>? data)` keyed by `chatId`. The built-in `MemoryCacheStateService` uses `IMemoryCache`. Custom implementations register via `UseState(s => s.UseCustom<MyService>())`.

Handlers mutate `Context.State` and `Context.StateData` directly; `StateMiddleware` persists changes after `next` returns.
