using Telepath.Models;

namespace Telepath.Routing;

public interface IRoute
{
    bool Matches(TelepathContext context);
    Task ExecuteAsync(IServiceProvider provider, TelepathContext context, CancellationToken ct);
}
