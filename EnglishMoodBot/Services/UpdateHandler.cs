using EnglishMoodBot.Handlers;
using EnglishMoodBot.State;
using EnglishMoodBot.State.Models;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace EnglishMoodBot.Services
{
    public sealed class UpdateHandler : IUpdateHandler
    {

        private readonly ITelegramBotClient _bot;
        private readonly StateService _states;
        private readonly List<IHandler> _handlers;

        public UpdateHandler(ITelegramBotClient bot, StateService states, IEnumerable<IHandler> handlers)
        {
            _bot = bot;
            _states = states;
            _handlers = handlers.ToList();
        }

        // ✅ новая сигнатура (Bot API v22) - без повторяющихся «_»
        public Task HandleErrorAsync(
         ITelegramBotClient botClient,
         Exception exception,
         HandleErrorSource source,
         CancellationToken ct)
        {
            Console.WriteLine($"TG error ({source}): {exception}");
            return Task.CompletedTask;
        }


        public async Task HandleUpdateAsync(
        ITelegramBotClient bot,
        Update update,
        CancellationToken ct)
        {
            Console.WriteLine($"▶ update: {update.Type}");//удалить позже

            // теперь НЕ фильтруем тип; отдаём всем зарегистрированным IHandler
            long chatId = update switch
            {
                { Message: { } m } => m.Chat.Id,
                { CallbackQuery: { } cb } => cb.Message!.Chat.Id,
                _ => 0
            };

            var state = chatId == 0 ? null : _states.Get(chatId);

            // ── глобальный guard: во время квиза игнорим любой ручной текст ──
            if (update.Message is { Type: MessageType.Text } msg
                && state.Step is >= QuizStep.Role and < QuizStep.Finished)
            {
                // проверим, есть ли такое текстовое значение в карте кнопок
                var opts = QuizHandler.DefaultMap[state.Step].Opts;
                if (!opts.Any(o => o.Trim().Equals(msg.Text.Trim(), StringComparison.OrdinalIgnoreCase)))
                    return; // не совпало ни с одной кнопкой → игнорируем
            }

            Console.WriteLine($"\n=== New update: Type={update.Type}, Chat={chatId}, Step={(state?.Step.ToString() ?? "null")} ===");
            if (update.Message != null)
                Console.WriteLine($"Message.Text: \"{update.Message.Text}\"");

            foreach (var h in _handlers)
            {
                bool can = h.CanHandle(update, state!);
                Console.WriteLine($"  Handler {h.GetType().Name}.CanHandle → {can}");
                if (can)
                {
                    Console.WriteLine($"    → Invoking {h.GetType().Name}.HandleAsync");
                    await h.HandleAsync(bot, update, state!, _states, ct);
                    break;
                }
            }
        }

    }
}
