using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EnglishMoodBot.State;
using EnglishMoodBot.State.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace EnglishMoodBot.Handlers
{
    internal sealed class CallbackHandler : IHandler
    {
        // Ловим только CallbackQuery
        public bool CanHandle(Update u, UserState _) =>
            u.CallbackQuery is not null;

        public async Task HandleAsync(
            ITelegramBotClient bot,
            Update u,
            UserState _,
            StateService __,
            CancellationToken ct)
        {
            // безопасно распаковываем
            if (u.CallbackQuery is null) return;
            var cb = u.CallbackQuery;

            long chat = cb.Message!.Chat.Id;

            if (cb.Data is "ticket_Tue" or "ticket_Thu")
            {
                const string caption = """
<b>💙 Твой персональный урок активирован!</b>

Поздравляем! Ты официально записан на бесплатный мини-урок и личную консультацию от Sherlock School. Это не скучная презентация для всех, а разговор конкретно о тебе и твоём английском.

Вот что будет на встрече:

— Как учить язык так, чтобы он был привычкой, а не стрессом.
— Какие подходы работают в жизни, а какие только красиво звучат.
— Как зумеры реально говорят на английском: от мемов до профессионального сленга.

<b>📣 С тобой будет работать опытный преподаватель Sherlock School,</b> который быстро определит твои сильные стороны и поможет наметить путь.

📍 <b>Формат:</b> онлайн-встреча (Zoom / Telegram)
🕖 <b>Старт</b> в выбранное тобой время (вторник или четверг).

В день встречи тебе придёт напоминание и ссылка для подключения.

До встречи!✨
<b>Sherlock School Team</b>
""";
                var path = Path.Combine(AppContext.BaseDirectory, "Assets", "Ticket.png");

                await using var fs = File.OpenRead(path);
                await bot.SendPhoto(chat,
                    InputFile.FromStream(fs,
                    "Ticket.png"),
                    caption,
                    parseMode: ParseMode.Html);

                await bot.AnswerCallbackQuery(cb.Id, "Билет отправлен 👆");
            }
        }
    }
}