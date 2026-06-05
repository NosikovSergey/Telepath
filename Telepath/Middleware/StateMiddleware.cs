using Telepath.Models;
using Telepath.Services;

namespace Telepath.Middleware;

internal class StateMiddleware : ITelepathMiddleware
{
    private readonly IStateService _stateService;

    public StateMiddleware(IStateService stateService)
    {
        _stateService = stateService;
    }

    public async Task InvokeAsync(TelepathContext context, Func<TelepathContext, CancellationToken, Task> next, CancellationToken cancellationToken)
    {
        if (context.ChatId.HasValue)
        {
            var (state, data) = await _stateService.GetAsync(context.ChatId.Value);
            context.State = state;
            context.StateData = data;
        }

        var stateBefore = context.State;
        var dataBefore = context.StateData;
        await next(context, cancellationToken);

        var stateChanged = !Equals(context.State, stateBefore);
        var dataChanged = context.StateData != null || dataBefore != null;

        if (context.ChatId.HasValue && (stateChanged || dataChanged))
            await _stateService.SetAsync(context.ChatId.Value, context.State, context.StateData);
    }
}
