using System.IO;
using EnglishMoodBot.Data;
using EnglishMoodBot.Services;
using EnglishMoodBot.State;
using EnglishMoodBot.State.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace EnglishMoodBot.Handlers
{
    internal sealed class StartCommandHandler : IHandler
    {
        private readonly Dictionary<QuizStep, (string Q, string[] Opts)> _map;
        private readonly ChecklistService _chk;   // понадобится, если решите сдвигать дальше
        private readonly BotDbContext _db;

        public StartCommandHandler(Dictionary<QuizStep, (string, string[])> map,
                                   ChecklistService chk, BotDbContext db)
        {
            _map = map;
            _chk = chk;
            _db = db;
        }

        public bool CanHandle(Update u, UserState _) => u.Message?.Text == "/start";

        public async Task HandleAsync(
            ITelegramBotClient bot,
            Update u,
            UserState state,
            StateService states,
            CancellationToken ct)
        {
            long chat = u.Message!.Chat.Id;

            // -1) чистим предыдущее состояние
            states.Reset(chat);
            state = states.Get(chat);

            // 0) Сохраняем или обновляем UserRecord:
            var user = await _db.Users.FindAsync(chat);
            if (user == null)
            {
                user = new UserRecord
                {
                    ChatId = chat,
                    UserName = u.Message.From?.Username,
                    FirstSeen = DateTime.UtcNow
                };
                _db.Users.Add(user);
            }
            else if (user.FirstSeen == default)
            {
                user.FirstSeen = DateTime.UtcNow;
                _db.Users.Update(user);
            }
            await _db.SaveChangesAsync(ct);

            // 1) приветственный текст
            const string welcome = """
<b>Добро пожаловать в SOVETNIK.</b>

Здесь начинается ваше знакомство с одной из самых надёжных инвестиционных команд в стране. Мы работаем там, где важна точность, расчёт и доверие. С теми, кто стремится не просто сохранить капитал, а использовать его как инструмент роста. Разрабатываем стратегии, помогаем управлять рисками, сопровождаем сделки, выстраиваем системный подход к деньгам.

SOVETNIK — это:

🏆 Лауреат премии «Проект года» в категории «Инвестиционное консультирование», обошли БКС, Финам и Альфу.
📊 Официальный участник общероссийской общественной организации «Инвестиционная Россия»
🧠 Нам доверяют предприниматели, руководители, частные инвесторы — те, кто делает выбор в пользу точных решений и прозрачной стратегии.
""";
            await bot.SendMessage(chat, welcome, parseMode: ParseMode.Html, cancellationToken: ct);

            // 2) PDF-презентация + пояснение
            const string more = """
<b>Хочешь узнать больше?</b>

Мы подготовили короткую презентацию, где собрали главное о «SOVETNIK»: наши подходы, принципы работы, направления, которые мы развиваем, и то, почему нам доверяют предприниматели, руководители и инвесторы по всей стране.
""";
            var pdf = Path.Combine(AppContext.BaseDirectory, "Assets", "Presentation.pdf");
            await bot.SendDocument(
                chat,
                InputFile.FromStream(File.OpenRead(pdf), "Presentation.pdf"),
                more,
                parseMode: ParseMode.Html,
                cancellationToken: ct);

            // 3) voice
            var voice = Path.Combine(AppContext.BaseDirectory, "Assets", "welcome.ogg");
            await bot.SendVoice(
                chat,
                InputFile.FromStream(File.OpenRead(voice), "welcome.ogg"),
                cancellationToken: ct);

            // 4) сразу запускаем квиз
            state.Step = QuizStep.Role;
            states.Save(chat, state);
            var (q, opts) = _map[QuizStep.Role];
            await bot.SendMessage(chat, q,
                parseMode: ParseMode.Html,
                replyMarkup: BuildReply(opts), cancellationToken: ct);
        }

        private static ReplyMarkup BuildReply(string[] opts) =>
            new ReplyKeyboardMarkup(opts.Select(o => new[] { new KeyboardButton(o) }))
            { ResizeKeyboard = true, OneTimeKeyboard = true };
    }
}
