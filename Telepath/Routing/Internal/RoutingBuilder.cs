namespace Telepath.Routing.Internal;

internal class RoutingBuilder : IRoutingBuilder
{
    private readonly List<IRoute> _routes = new();

    public void AddRoute(IRoute route)
    {
        _routes.Add(route);
    }

    public ICollection<IRoute> Build()
    {
        return _routes;
    }
}
