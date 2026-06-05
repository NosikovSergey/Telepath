using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InlineQueryResults;

namespace Telepath.Routing.Handlers;

public abstract class TelepathInlineQueryHandler : TelepathHandler
{
    protected sealed override Task HandleAsync()
    {
        return HandleAsync(Context.Update.InlineQuery!);
    }

    protected abstract Task HandleAsync(InlineQuery query);

    protected Task AnswerInlineQueryAsync(
        IEnumerable<InlineQueryResult> results,
        string? nextOffset = null,
        bool isPersonal = false,
        int? cacheTime = null)
    {
        return Bot.AnswerInlineQuery(
            Context.Update.InlineQuery!.Id,
            results,
            cacheTime,
            isPersonal,
            nextOffset,
            cancellationToken: CancellationToken);
    }
}
