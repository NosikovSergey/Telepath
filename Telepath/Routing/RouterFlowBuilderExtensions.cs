using System.Text.RegularExpressions;
using Telepath.Models;
using Telepath.Routing.Handlers;
using Telepath.Routing.Internal;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Telepath.Routing;

public static class RouterFlowBuilderExtensions
{
    public static IRoutingBuilder State<T>(this IRoutingBuilder builder, params Enum[] states)
        where T : IRouterHandler
    {
        return builder.When<T>(ctx => ctx.State != null && states.Contains(ctx.State));
    }

    public static IRoutingBuilder Command<T>(this IRoutingBuilder builder, params string[] commands)
        where T : IRouterHandler
    {
        return builder.When<T>(ctx =>
        {
            if (!IsMessage(ctx.Update))
                return false;

            var parsed = ParseCommand(ctx.Update.Message!.Text!);
            if (parsed == null) return false;
            if (parsed.Value.Mention != null && ctx.BotUser != null &&
                !string.Equals(parsed.Value.Mention, ctx.BotUser.Username, StringComparison.OrdinalIgnoreCase))
                return false;

            return commands.Any(cmd => string.Equals(parsed.Value.Command, cmd, StringComparison.OrdinalIgnoreCase));
        });
    }

    public static IRoutingBuilder Command<T>(this IRoutingBuilder builder, Enum state, params string[] commands)
        where T : IRouterHandler
        => builder.Command<T>(new[] { state }, commands);

    public static IRoutingBuilder Command<T>(this IRoutingBuilder builder, Enum[] states, params string[] commands)
        where T : IRouterHandler
    {
        return builder.When<T>(ctx =>
        {
            if (ctx.State == null || !states.Contains(ctx.State) || !IsMessage(ctx.Update))
                return false;

            var parsed = ParseCommand(ctx.Update.Message!.Text!);
            if (parsed == null) return false;
            if (parsed.Value.Mention != null && ctx.BotUser != null &&
                !string.Equals(parsed.Value.Mention, ctx.BotUser.Username, StringComparison.OrdinalIgnoreCase))
                return false;

            return commands.Any(cmd => string.Equals(parsed.Value.Command, cmd, StringComparison.OrdinalIgnoreCase));
        });
    }

    public static IRoutingBuilder Message<T>(this IRoutingBuilder builder, Func<string, bool> func)
        where T : IRouterHandler
    {
        return builder.When<T>(ctx =>
        {
            if (!IsMessage(ctx.Update))
                return false;

            return func(ctx.Update.Message!.Text!);
        });
    }

    public static IRoutingBuilder Message<T>(this IRoutingBuilder builder, params string[] values)
        where T : IRouterHandler
    {
        return builder.Message<T>(text =>
            values.Any(pattern => string.Equals(text, pattern, StringComparison.OrdinalIgnoreCase)));
    }

    public static IRoutingBuilder Message<T>(this IRoutingBuilder builder)
        where T : IRouterHandler
    {
        return builder.Message<T>(_ => true);
    }

    public static IRoutingBuilder Message<T>(this IRoutingBuilder builder, Enum state)
        where T : IRouterHandler
    {
        return builder.When<T>(ctx => Equals(ctx.State, state) && IsMessage(ctx.Update));
    }

    public static IRoutingBuilder Message<T>(this IRoutingBuilder builder, Enum state, Func<string, bool> func)
        where T : IRouterHandler
    {
        return builder.When<T>(ctx =>
        {
            if (!Equals(ctx.State, state) || !IsMessage(ctx.Update))
                return false;
            return func(ctx.Update.Message!.Text!);
        });
    }

    public static IRoutingBuilder Message<T>(this IRoutingBuilder builder, Enum state, params string[] values)
        where T : IRouterHandler
    {
        return builder.Message<T>(state, text =>
            values.Any(v => string.Equals(text, v, StringComparison.OrdinalIgnoreCase)));
    }

    public static IRoutingBuilder Message<T>(this IRoutingBuilder builder, Func<TelepathContext, string[]> patternsBuilder)
        where T : IRouterHandler
    {
        return builder.When<T>(ctx =>
        {
            if (!IsMessage(ctx.Update))
                return false;

            var patterns = patternsBuilder(ctx);
            return patterns.Any(pattern =>
                string.Equals(ctx.Update.Message!.Text, pattern, StringComparison.OrdinalIgnoreCase));
        });
    }

    public static IRoutingBuilder MessageRegex<T>(this IRoutingBuilder builder, params string[] regexes)
        where T : IRouterHandler
    {
        var compiled = regexes.Select(p => new Regex(p, RegexOptions.IgnoreCase | RegexOptions.Compiled)).ToArray();
        return builder.Message<T>(text => compiled.Any(rx => rx.IsMatch(text)));
    }

    public static IRoutingBuilder Callback<THandler, TCallbackData>(
        this IRoutingBuilder builder,
        Func<TCallbackData, bool>? predicate = null)
        where THandler : IRouterHandler
        where TCallbackData : CallbackData, new()
    {
        var filter = predicate is not null
            ? CallbackData.CreateFilter(predicate)
            : CallbackData.CreateFilter<TCallbackData>();

        return builder.Callback<THandler>(filter);
    }

    public static IRoutingBuilder Callback<T>(this IRoutingBuilder builder) where T : IRouterHandler
        => builder.Callback<T>(_ => true);

    public static IRoutingBuilder Callback<T>(this IRoutingBuilder builder, params string[] values)
        where T : IRouterHandler
    {
        return builder.Callback<T>(data =>
            values.Any(v => string.Equals(data, v, StringComparison.OrdinalIgnoreCase)));
    }

    public static IRoutingBuilder Callback<T>(this IRoutingBuilder builder, Func<string, bool> match)
        where T : IRouterHandler
    {
        builder.When<T>(ctx =>
        {
            if (!IsCallBack(ctx.Update))
                return false;

            var data = ctx.Update.CallbackQuery?.Data;
            return !string.IsNullOrEmpty(data) && match(data);
        });

        return builder;
    }

    public static IRoutingBuilder CallbackRegex<T>(this IRoutingBuilder builder, params string[] patterns)
        where T : IRouterHandler
    {
        var compiled = patterns.Select(p => new Regex(p, RegexOptions.IgnoreCase | RegexOptions.Compiled)).ToArray();
        return builder.Callback<T>(data => compiled.Any(rx => rx.IsMatch(data)));
    }

    public static IRoutingBuilder When<T>(this IRoutingBuilder builder, Predicate<TelepathContext> predicate)
        where T : IRouterHandler
    {
        builder.AddRoute(new Route<T>(predicate));
        return builder;
    }

    public static IRoutingBuilder WhenUpdateType<T>(this IRoutingBuilder builder, params UpdateType[] types)
        where T : IRouterHandler
    {
        return builder.When<T>(ctx => types.Any(type => type.Equals(ctx.Update.Type)));
    }

    public static IRoutingBuilder WhenMessageType<T>(this IRoutingBuilder builder, params MessageType[] types)
        where T : IRouterHandler
    {
        return builder.When<T>(ctx =>
            ctx.Update.Type == UpdateType.Message &&
            types.Any(type => type.Equals(ctx.Update.Message!.Type)));
    }

    public static IRoutingBuilder PreCheckout<T>(this IRoutingBuilder builder, Predicate<TelepathContext> predicate)
        where T : IRouterHandler
    {
        return builder.When<T>(ctx => ctx.Update.Type == UpdateType.PreCheckoutQuery && predicate(ctx));
    }

    public static IRoutingBuilder PreCheckout<T>(this IRoutingBuilder builder)
        where T : IRouterHandler
    {
        return builder.When<T>(ctx => ctx.Update.Type == UpdateType.PreCheckoutQuery);
    }

    private static bool IsMessage(Update update) =>
        update.Type == UpdateType.Message && !string.IsNullOrEmpty(update.Message?.Text);

    private static bool IsCallBack(Update update) =>
        update.Type == UpdateType.CallbackQuery && update.CallbackQuery != null;

    private static (string Command, string? Mention)? ParseCommand(string text)
    {
        if (!text.StartsWith('/'))
            return null;

        var commandPart = text.Split(' ')[0][1..]; // strip leading '/', drop args
        var atIndex = commandPart.IndexOf('@');

        return atIndex >= 0
            ? (commandPart[..atIndex], commandPart[(atIndex + 1)..])
            : (commandPart, null);
    }
}
