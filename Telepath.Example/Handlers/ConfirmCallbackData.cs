using Telepath.Routing;

namespace Telepath.Example.Handlers;

[CallbackPrefix("confirm")]
public class ConfirmCallbackData : CallbackData
{
    public bool Confirmed { get; set; }
}
