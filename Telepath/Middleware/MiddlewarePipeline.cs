using Telepath.Models;

namespace Telepath.Middleware;

internal class MiddlewarePipeline
{
    private readonly Func<TelepathContext, CancellationToken, Task> _pipeline;

    public MiddlewarePipeline(IEnumerable<ITelepathMiddleware> middlewares)
    {
        Func<TelepathContext, CancellationToken, Task> next = (_, _) => Task.CompletedTask;

        foreach (var middleware in middlewares.Reverse())
        {
            var current = middleware;
            var prevNext = next;
            next = (ctx, ct) => current.InvokeAsync(ctx, prevNext, ct);
        }

        _pipeline = next;
    }

    public Task InvokeAsync(TelepathContext context, CancellationToken cancellationToken)
    {
        return _pipeline(context, cancellationToken);
    }
}
