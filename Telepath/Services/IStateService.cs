namespace Telepath.Services;

public interface IStateService
{
    Task<(Enum? State, Dictionary<string, string>? Data)> GetAsync(long chatId);
    Task SetAsync(long chatId, Enum? state, Dictionary<string, string>? data);
}
