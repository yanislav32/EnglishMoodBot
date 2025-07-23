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
                string userTgLink = "https://t.me/{user.UserName}";
                var msgNewUser =
                                $"<b>Новый пользователь!</b>\n" +
                                $"Username: <a href=\"https://t.me/{user.UserName}\">@{user.UserName}</a>\n" +
                                $"Id: <code>{user.ChatId}</code>\n" +
                                $"Дата подключения: {user.FirstSeen:dd-MM-yyyy}\n" +
                                $"Время подключения: {user.FirstSeen:HH:mm:ss}";
                long adminChatId = 528017102;
                await bot.SendMessage(adminChatId, msgNewUser, parseMode: ParseMode.Html);
                await bot.SendMessage(406865885, msgNewUser, parseMode: ParseMode.Html);
                
            }
            else if (user.FirstSeen == default)
            {
                user.FirstSeen = DateTime.UtcNow;
                _db.Users.Update(user);
            }
            await _db.SaveChangesAsync(ct);

            // 1) приветственный текст
            const string welcome = """
<b>Добро пожаловать в Sherlock School!</b>

Ты оказался(лась) в месте, где английский перестаёт быть школьным предметом и становится твоим инструментом — для работы, путешествий, общения, контента и новых возможностей.

<i><u>Sherlock School — это:</u></i>

⚡️ Школа, где учат не зазубривать, а понимать язык
⚡️ Эксперты, которые помогают видеть разницу между учебником и живой речью
⚡️ Сообщество, где можно практиковаться, ошибаться, задавать вопросы и расти
""";
            var photo = Path.Combine(AppContext.BaseDirectory, "Assets", "Photo.jpeg");
            await bot.SendPhoto(
                chat,
                InputFile.FromStream(File.OpenRead(photo), "Photo.jpeg"),
                welcome, 
                parseMode: ParseMode.Html, 
                cancellationToken: ct);

            // 2) PDF-презентация + пояснение
            const string more = """
<b>Мы верим, что английский — это не уровень в сертификате</b>, а часть жизни. Здесь учат понимать, а не просто переводить. Помогают не бояться ошибок. Показывают, как звучит язык вне учебных примеров — в песнях, фильмах, мемах, разговорах.

У нас ты найдёшь пространство, где тебя слышат, поддерживают и подбирают обучение под твой ритм. Без гонки, без давления, зато с юмором, пользой и удовольствием. <b>Добро пожаловать!</b>
""";
            /*var pdf = Path.Combine(AppContext.BaseDirectory, "Assets", "Presentation.pdf");
            await bot.SendDocument(
                chat,
                InputFile.FromStream(File.OpenRead(pdf), "Presentation.pdf"),
                more,
                parseMode: ParseMode.Html,
                cancellationToken: ct);
            */
            await bot.SendMessage(chat,more, parseMode: ParseMode.Html, cancellationToken: ct);

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
