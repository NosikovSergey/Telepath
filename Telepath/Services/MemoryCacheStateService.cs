using Microsoft.Extensions.Caching.Memory;

namespace Telepath.Services;

internal class MemoryCacheStateService : IStateService
{
    private readonly IMemoryCache _cache;

    public MemoryCacheStateService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public Task<(Enum? State, Dictionary<string, string>? Data)> GetAsync(long chatId)
    {
        _cache.TryGetValue<StateEntry>($"state:{chatId}", out var entry);
        return Task.FromResult<(Enum?, Dictionary<string, string>?)>((entry?.State, entry?.Data));
    }

    public Task SetAsync(long chatId, Enum? state, Dictionary<string, string>? data)
    {
        if (state == null && data == null)
            _cache.Remove($"state:{chatId}");
        else
            _cache.Set($"state:{chatId}", new StateEntry(state, data));

        return Task.CompletedTask;
    }

    private record StateEntry(Enum? State, Dictionary<string, string>? Data);
}
