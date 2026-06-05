using Telepath.Models;
using Telegram.Bot.Types;

namespace Telepath.Middleware;

internal class StartDataMetadataMiddleware : ITelepathMiddleware
{
    private const string MetadataKey = "StartData";

    public async Task InvokeAsync(
        TelepathContext context,
        Func<TelepathContext, CancellationToken, Task> next,
        CancellationToken cancellationToken)
    {
        var startParam = GetStartParam(context.Update);
        if (!string.IsNullOrWhiteSpace(startParam))
        {
            var parsed = ParseStartData(startParam);
            if (parsed.Any())
            {
                context.Metadata ??= new Dictionary<string, object>();
                context.Metadata[MetadataKey] = parsed;
            }
        }

        await next(context, cancellationToken);
    }

    private string? GetStartParam(Update update)
    {
        var message = update.Message;
        if (message is null || string.IsNullOrWhiteSpace(message.Text))
            return null;

        if (!message.Text.StartsWith("/start", StringComparison.OrdinalIgnoreCase))
            return null;

        var parts = message.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2 ? parts[1] : null;
    }

    private Dictionary<string, string> ParseStartData(string param)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var decoded = Uri.UnescapeDataString(param);

        var pairs = decoded.Split(';', StringSplitOptions.RemoveEmptyEntries);
        foreach (var pair in pairs)
        {
            var kv = pair.Split('=', 2);
            if (kv.Length == 2 && !string.IsNullOrWhiteSpace(kv[0]))
                result[kv[0].Trim()] = kv[1].Trim();
        }

        return result;
    }
}
