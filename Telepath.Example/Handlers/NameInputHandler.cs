using Telepath.Routing.Handlers;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace Telepath.Example.Handlers;

public class NameInputHandler : TelepathHandler
{
    protected override async Task HandleAsync()
    {
        var name = Context.Update.Message!.Text!;

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("Да", "confirm:yes") },
            new[] { InlineKeyboardButton.WithCallbackData("Нет", "confirm:no") }
        });

        await Bot.SendMessage(
            Context.ChatId!.Value,
            $"Рад познакомиться, {name}! Тебе нравится этот бот?",
            replyMarkup: keyboard,
            cancellationToken: CancellationToken);

        Context.State = null;
    }
}
