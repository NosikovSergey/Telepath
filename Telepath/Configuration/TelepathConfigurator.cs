using Telepath.Hosting;
using Telepath.Middleware;
using Telepath.Processing;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;

namespace Telepath.Configuration;

internal class TelepathConfigurator : ITelepathBuilder
{
    private readonly List<Tuple<Type, Func<IServiceProvider, ITelepathMiddleware>>> _factories = new();
    private BotSettings? _settings;
    private Action<StateConfigurator>? _stateAction;

    public ITelepathBuilder Configure(BotSettings settings)
    {
        _settings = settings;
        return this;
    }

    public ITelepathBuilder Use<T>() where T : ITelepathMiddleware
    {
        _factories.Add(new(typeof(T), sp => sp.GetRequiredService<T>()));
        return this;
    }

    public ITelepathBuilder Use<T>(Func<IServiceProvider, T> factory) where T : ITelepathMiddleware
    {
        _factories.Add(new(typeof(T), sp => factory(sp)));
        return this;
    }

    public ITelepathBuilder UseState(Action<StateConfigurator> configure)
    {
        _stateAction = configure;
        return this;
    }

    public void Register(IServiceCollection services)
    {
        if (_settings == null)
            throw new InvalidOperationException("You must call Configure before configuring the bot.");

        services.AddSingleton<ITelegramBotClient>(_ => new TelegramBotClient(_settings.TelegramBotToken));
        services.AddSingleton<BotInfoMiddleware>();
        services.AddSingleton<AnswerCallbackQueryMiddleware>();

        if (_stateAction != null)
        {
            services.AddSingleton<StateMiddleware>();
            _stateAction(new StateConfigurator(services));
        }

        var middlewareTypes = _factories.Select(f => f.Item1).Distinct();
        foreach (var type in middlewareTypes)
            services.AddSingleton(type);

        services.AddSingleton<MiddlewarePipeline>(sp =>
        {
            var coreMiddlewares = new List<ITelepathMiddleware>
            {
                sp.GetRequiredService<BotInfoMiddleware>(),
                sp.GetRequiredService<AnswerCallbackQueryMiddleware>()
            };

            if (_stateAction != null)
                coreMiddlewares.Add(sp.GetRequiredService<StateMiddleware>());

            var userMiddlewares = _factories.Select(i => i.Item2.Invoke(sp));
            return new MiddlewarePipeline(coreMiddlewares.Concat(userMiddlewares));
        });

        services.AddSingleton<ITelepathProcessor, TelepathProcessor>();

        if (_settings.Transport == TelepathTransportMode.Webhook && string.IsNullOrWhiteSpace(_settings.WebhookUri))
            throw new InvalidOperationException("WebhookUri is required for webhook mode.");

        services.AddSingleton(_ => new TelepathInitializerSettings
        {
            WebhookUrl = _settings.WebhookUri,
            WebhookSecretToken = _settings.WebhookSecret
        });

        switch (_settings.Transport)
        {
            case TelepathTransportMode.LongPolling:
                services.AddHostedService<LongPollingInitializer>();
                break;
            case TelepathTransportMode.Webhook:
                services.AddHostedService<WebhookInitializer>();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
