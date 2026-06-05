using Telegram.Bot;

namespace Telepath.Hosting;

internal class WebhookInitializer : TelepathInitializer
{
    private readonly ITelegramBotClient _client;
    private readonly TelepathInitializerSettings _settings;

    public WebhookInitializer(ITelegramBotClient client, TelepathInitializerSettings settings) : base()
    {
        _client = client;
        _settings = settings;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await base.StartAsync(cancellationToken);
        await _client.SetWebhook(_settings.WebhookUrl!, secretToken: _settings.WebhookSecretToken, cancellationToken: cancellationToken);
    }
}
