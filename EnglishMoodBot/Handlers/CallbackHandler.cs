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
<b>🎟 Ваш персональный e-билет активирован.</b>

Вы приглашены на закрытую инвестиционную сессию “Советника” — встречу, куда попадают только владельцы такого билета. Формат не для массовой публики: одна вечерняя сессия, доступ к аналитике и стратегиям, которые обычно остаются «за кадром».

Вы услышите:
<blockquote>— Как аналитики работают с нестабильными активами
— Какие сигналы в цене видят только опытные трейдеры
— Что на самом деле важно при принятии решений</blockquote>

📣 Спикеры:

<b>— Сергей Рыбин</b>
Частный трейдер с глубоким опытом на валютных, товарных, фондовых и крипторынках.

<b>— Александр Миллер</b>
Ведущий эксперт и бывший сотрудник Morgan Stanley. Комментатор рынка

🕖 <i><u>Старт ровно в 18:00.</u></i>

До встречи на мероприятии.
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