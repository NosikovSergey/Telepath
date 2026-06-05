namespace Telepath.Routing.Handlers;

public abstract class TelepathCallbackHandler<TCallbackData> : TelepathHandler where TCallbackData : CallbackData, new()
{
    protected sealed override Task HandleAsync()
    {
        var data = Context.Update.CallbackQuery!.Data!;
        var callbackData = CallbackData.Deserialize<TCallbackData>(data);
        return HandleAsync(callbackData);
    }

    protected abstract Task HandleAsync(TCallbackData callbackData);
}
