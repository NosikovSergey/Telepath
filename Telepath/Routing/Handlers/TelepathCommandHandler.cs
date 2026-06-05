namespace Telepath.Routing.Handlers;

public abstract class TelepathCommandHandler : TelepathHandler
{
    protected sealed override Task HandleAsync()
    {
        var parts = Context.Update.Message!.Text!.Split(' ');
        var commandPart = parts[0][1..]; // strip leading '/'
        var atIndex = commandPart.IndexOf('@');
        var command = atIndex >= 0 ? commandPart[..atIndex] : commandPart;
        var args = parts.Skip(1).ToArray();
        return HandleAsync(command, args);
    }

    protected abstract Task HandleAsync(string command, string[] args);
}
