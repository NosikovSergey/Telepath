using Telepath.Configuration;
using Telepath.Example;
using Telepath.Example.Handlers;
using Telepath.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        services.AddTransient<StartHandler>();
        services.AddTransient<NameInputHandler>();
        services.AddTransient<ConfirmCallbackHandler>();

        var token = ctx.Configuration["Telegram:Token"]
            ?? throw new InvalidOperationException("Telegram:Token is required in appsettings.json");

        
        services.AddTelepath(bot =>
        {
            bot.Configure(new BotSettings
            {
                TelegramBotToken = token,
                Transport = TelepathTransportMode.LongPolling
            });

            bot.UseState(state => state.UseMemoryCache());

            bot.UseRouter(router =>
            {
                router.Command<StartHandler>("start");
                router.Message<NameInputHandler>(BotState.WaitingForName);
                router.Callback<ConfirmCallbackHandler, ConfirmCallbackData>();
            });
        });
    })
    .Build();

await host.RunAsync();
