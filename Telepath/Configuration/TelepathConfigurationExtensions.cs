using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Telepath.Middleware;
using Telepath.Processing;
using Telepath.Routing;
using Telepath.Routing.Handlers;
using Telepath.Routing.Internal;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Types;

namespace Telepath.Configuration;

public static class TelepathConfigurationExtensions
{
    public static IEndpointRouteBuilder MapTelepathWebhook(this IEndpointRouteBuilder endpoints, string pattern, string secretToken)
    {
        ArgumentNullException.ThrowIfNull(pattern, nameof(pattern));
        if (string.IsNullOrWhiteSpace(secretToken))
            throw new ArgumentException("Secret token must not be null or whitespace.", nameof(secretToken));

        var secretBytes = Encoding.UTF8.GetBytes(secretToken);

        endpoints.MapPost(pattern, async (
            [FromBody] Update update,
            [FromHeader(Name = "X-Telegram-Bot-Api-Secret-Token")] string token,
            [FromServices] ITelepathProcessor processor,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrEmpty(token))
                return Results.Unauthorized();

            var tokenBytes = Encoding.UTF8.GetBytes(token);

            if (!CryptographicOperations.FixedTimeEquals(tokenBytes, secretBytes))
                return Results.Unauthorized();

            await processor.HandleUpdate(update, cancellationToken);
            return Results.Ok();
        });

        return endpoints;
    }

    public static IServiceCollection AddTelepath(this IServiceCollection services, Action<ITelepathBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var builder = new TelepathConfigurator();
        configure(builder);
        builder.Register(services);
        return services;
    }

    public static IServiceCollection AddRouter(this IServiceCollection services, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        services.AddTransient<IRoutingBuilder, RoutingBuilder>();

        var types = assembly
            .GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Where(t => typeof(IRouterHandler).IsAssignableFrom(t))
            .ToList();

        foreach (var type in types)
            services.AddTransient(type);

        return services;
    }

    public static ITelepathBuilder UseErrorLogging(this ITelepathBuilder builder)
    {
        builder.Use<ErrorLoggingMiddleware>();
        return builder;
    }

    public static ITelepathBuilder UseRequestLogging(this ITelepathBuilder builder)
    {
        builder.Use<RequestLoggingMiddleware>();
        return builder;
    }

    public static ITelepathBuilder UseRouter(this ITelepathBuilder builder, Action<IRoutingBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        builder.Use<TelepathRouterMiddleware>(sp =>
        {
            var routingBuilder = new RoutingBuilder();
            configure(routingBuilder);
            var routes = routingBuilder.Build();
            return ActivatorUtilities.CreateInstance<TelepathRouterMiddleware>(sp, routes);
        });
        return builder;
    }
}
