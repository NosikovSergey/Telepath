using Telepath.Routing.Handlers;
using Telegram.Bot;

namespace Telepath.Example.Handlers;

public class ConfirmCallbackHandler : TelepathCallbackHandler<ConfirmCallbackData>
{
    protected override async Task HandleAsync(ConfirmCallbackData data)
    {
        var reply = data.Confirmed ? "Отлично! 🎉" : "Жаль... но мы будем стараться!";

        await Bot.SendMessage(Context.ChatId!.Value, reply, cancellationToken: CancellationToken);
        await AnswerCallbackQueryAsync();
    }
}
