using Telegram.Bot.Types;

namespace Telepath.Processing;

public interface ITelepathProcessor
{
    Task HandleUpdate(Update update, CancellationToken cancellationToken);
}
