namespace Telepath.Routing.Handlers;

public abstract class TelepathMessageHandler : TelepathHandler
{
    protected sealed override Task HandleAsync()
    {
        return HandleAsync(Context.Update.Message!.Text!);
    }

    protected abstract Task HandleAsync(string text);
}
