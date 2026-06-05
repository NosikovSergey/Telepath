using Telepath.Processing;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace Telepath.Hosting;

internal class LongPollingInitializer : TelepathInitializer
{
    private readonly ITelegramBotClient _client;
    private readonly ITelepathProcessor _processor;
    private readonly ILogger<LongPollingInitializer> _logger;

    public LongPollingInitializer(
        ITelegramBotClient client,
        ITelepathProcessor processor,
        ILogger<LongPollingInitializer> logger) : base()
    {
        _client = client;
        _processor = processor;
        _logger = logger;
    }

    private Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken ct)
    {
        return _processor.HandleUpdate(update, ct);
    }

    private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken ct)
    {
        _logger.LogError(exception, "Telegram API error: {Message}", exception.Message);
        return Task.CompletedTask;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await base.StartAsync(cancellationToken);
        await _client.DeleteWebhook(false, cancellationToken);

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = [],
        };

        _client.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            cancellationToken: cancellationToken
        );
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        await _client.Close(cancellationToken: cancellationToken);
    }
}
