using Telepath.Routing.Handlers;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace Telepath.Example.Handlers;

public class StartHandler : TelepathHandler
{
    protected override async Task HandleAsync()
    {
        await Bot.SendMessage(Context.ChatId!.Value, "Привет! Как тебя зовут?", cancellationToken: CancellationToken);
        Context.State = BotState.WaitingForName;
    }
}
