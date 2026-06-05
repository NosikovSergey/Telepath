namespace Telepath.Routing;

public interface IRoutingBuilder
{
    void AddRoute(IRoute route);
    ICollection<IRoute> Build();
}
