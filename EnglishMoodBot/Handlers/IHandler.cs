using System.Threading;
using System.Threading.Tasks;
using EnglishMoodBot.State;
using EnglishMoodBot.State.Models;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace EnglishMoodBot.Handlers
{
    /// <summary>Базовый интерфейс любого обработчика апдейтов.</summary>
    public interface IHandler
    {
        bool CanHandle(Update update, UserState state);

        Task HandleAsync(
            ITelegramBotClient bot,
            Update update,
            UserState state,
            StateService states,
            CancellationToken ct);
    }
}
