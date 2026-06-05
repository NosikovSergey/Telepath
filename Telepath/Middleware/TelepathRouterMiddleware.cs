using Telepath.Models;
using Telepath.Routing;

namespace Telepath.Middleware;

internal class TelepathRouterMiddleware : ITelepathMiddleware
{
    private readonly IServiceProvider _sp;
    private readonly IEnumerable<IRoute> _entries;

    public TelepathRouterMiddleware(IServiceProvider sp, IEnumerable<IRoute> entries)
    {
        _sp = sp;
        _entries = entries;
    }

    public async Task InvokeAsync(TelepathContext context, Func<TelepathContext, CancellationToken, Task> next, CancellationToken cancellationToken)
    {
        var invoked = false;
        foreach (var entry in _entries)
        {
            if (entry.Matches(context))
            {
                await entry.ExecuteAsync(_sp, context, cancellationToken);
                invoked = true;
                break;
            }
        }

        if (!invoked)
            await next(context, cancellationToken);
    }
}
