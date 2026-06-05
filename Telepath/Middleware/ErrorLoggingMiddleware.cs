using Telepath.Models;
using Microsoft.Extensions.Logging;

namespace Telepath.Middleware;

internal class ErrorLoggingMiddleware : ITelepathMiddleware
{
    private readonly ILogger<ErrorLoggingMiddleware> _logger;

    public ErrorLoggingMiddleware(ILogger<ErrorLoggingMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task InvokeAsync(TelepathContext context, Func<TelepathContext, CancellationToken, Task> next,
        CancellationToken cancellationToken)
    {
        try
        {
            await next(context, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled middleware error");
            throw;
        }
    }
}
