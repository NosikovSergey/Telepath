using Telepath.Middleware;
using Telepath.Models;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace Telepath.Processing;

internal class TelepathProcessor : ITelepathProcessor
{
    private readonly MiddlewarePipeline _pipeline;
    private readonly ILogger<TelepathProcessor> _logger;

    public TelepathProcessor(MiddlewarePipeline pipeline, ILogger<TelepathProcessor> logger)
    {
        _pipeline = pipeline;
        _logger = logger;
    }

    public async Task HandleUpdate(Update update, CancellationToken cancellationToken)
    {
        try
        {
            var context = new TelepathContext
            {
                Update = update
            };

            await _pipeline.InvokeAsync(context, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Handle updates error");
        }
    }
}
