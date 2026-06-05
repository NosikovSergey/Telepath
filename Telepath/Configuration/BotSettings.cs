namespace Telepath.Configuration;

public class BotSettings
{
    public string TelegramBotToken { get; set; } = null!;
    public TelepathTransportMode Transport { get; set; }
    public string? WebhookUri { get; set; }
    public string? WebhookSecret { get; set; }
}
