using EnglishMoodBot.Data;
using EnglishMoodBot.Handlers;
using EnglishMoodBot.Services;
using EnglishMoodBot.State;
using EnglishMoodBot.State.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Telegram.Bot.Polling;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((ctx, cfg) =>
    {
        // независимо от среды
        cfg.AddUserSecrets<Program>(optional: true);
    })
    .ConfigureServices((context, services) =>
    {
        // context.Configuration уже включает:
        // • appsettings*.json
        // • user-secrets (если среда Development)
        // • переменные окружения
        var cfg = context.Configuration;
        var conn = cfg.GetConnectionString("BotDb");
        //Console.WriteLine($"[DEBUG] TG_TOKEN = {cfg["TG_TOKEN"]}");

        var token = cfg["TG_TOKEN"]                         // User Secrets / appsettings
                 ?? Environment.GetEnvironmentVariable("TG_TOKEN") // env-var
                 ?? throw new InvalidOperationException("TG_TOKEN missing");

        services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(token));

        services.AddSingleton<StateService>();
        services.AddSingleton<ChecklistService>();



        services.AddDbContext<BotDbContext>(opt =>
            opt.UseNpgsql(conn));

        // создаём единственный словарь map и шарим его
        var map = new Dictionary<QuizStep, (string, string[])>(QuizHandler.DefaultMap);
        services.AddSingleton(map);

        services.AddSingleton<IHandler, StartCommandHandler>();
        services.AddSingleton<IHandler, CallbackHandler>();
        services.AddSingleton<IHandler, QuizHandler>();

        services.AddSingleton<IUpdateHandler, UpdateHandler>();
        services.AddHostedService<BotBackgroundService>();
    })
    .Build();

using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
    db.Database.Migrate();

    // сюда же помещаем "SetMyCommandsAsync"
    var bot = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();

    var commands = new[]
    {
        new Telegram.Bot.Types.BotCommand { Command = "start", Description = "Запустить бота" }
        // сюда можно добавить любые другие ваши команды
    };

    await bot.SetMyCommands(
        commands: commands,
        scope: new Telegram.Bot.Types.BotCommandScopeDefault()
    );
    // 2) Делаем кнопку меню, которая в любой момент разворачивает список команд
    await bot.SetChatMenuButton(
        menuButton: new Telegram.Bot.Types.MenuButtonCommands(),
        cancellationToken: CancellationToken.None);
}

await host.RunAsync();

