using Microsoft.Extensions.Hosting;

namespace Telepath.Hosting;

internal abstract class TelepathInitializer : IHostedService
{
    public virtual Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public virtual Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
