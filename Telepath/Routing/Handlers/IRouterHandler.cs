using Telepath.Models;
using Telegram.Bot;

namespace Telepath.Routing.Handlers;

public interface IRouterHandler
{
    Task ExecuteAsync(TelepathContext context, ITelegramBotClient bot, CancellationToken cancellationToken);
}
