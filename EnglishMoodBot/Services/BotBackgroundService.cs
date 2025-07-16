using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

namespace EnglishMoodBot.Services
{
    public sealed class BotBackgroundService : BackgroundService
    {
        private readonly ITelegramBotClient _bot;
        private readonly IUpdateHandler _handler;

        public BotBackgroundService(ITelegramBotClient bot, IUpdateHandler handler)
        {
            _bot = bot;
            _handler = handler;
        }

        // BotBackgroundService.cs
        protected override Task ExecuteAsync(CancellationToken stop)
        {
            _bot.StartReceiving(_handler,          // ← один аргумент
                receiverOptions: new ReceiverOptions()
                {

                },
                cancellationToken: stop);

            return Task.CompletedTask;
        }

    }

}
