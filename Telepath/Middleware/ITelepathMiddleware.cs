using Telepath.Models;

namespace Telepath.Middleware;

public interface ITelepathMiddleware
{
    Task InvokeAsync(TelepathContext context, Func<TelepathContext, CancellationToken, Task> next, CancellationToken cancellationToken);
}
