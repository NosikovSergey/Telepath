using System.Text.Json;
using Telepath.Models;
using Microsoft.Extensions.Logging;

namespace Telepath.Middleware;

internal class RequestLoggingMiddleware : ITelepathMiddleware
{
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(ILogger<RequestLoggingMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task InvokeAsync(
        TelepathContext context,
        Func<TelepathContext, CancellationToken, Task> next,
        CancellationToken cancellationToken)
    {
        if (_logger.IsEnabled(LogLevel.Trace))
        {
            var json = JsonSerializer.Serialize(context.Update);
            _logger.LogTrace("Received update: {Update}", json);
        }
        else if (_logger.IsEnabled(LogLevel.Debug))
        {
            var updateType = context.Update.Type;
            var data = context.Update.Message?.Text
                       ?? context.Update.CallbackQuery?.Data
                       ?? context.Update.InlineQuery?.Query
                       ?? string.Empty;
            _logger.LogDebug(
                "Update type: {Type} from user {UserId} chat {ChatId} data: {Data}",
                updateType,
                context.UserId,
                context.ChatId,
                data);
        }

        await next(context, cancellationToken);
    }
}
