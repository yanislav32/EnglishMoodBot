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
    internal sealed class QuizHandler : IHandler
    {
        // ❶ Карта «шаг → (вопрос, кнопки)»
        public static readonly Dictionary<QuizStep, (string Q, string[] Opts)> DefaultMap = new()
        {
            [QuizStep.Role] = (
    """
<b>🤬 Ты точно учишь так, как подходит именно тебе?</b>

Большинство думают, что дело в том, сколько учить английский.
На самом деле — в том, как.

- Один лучше схватывает через разговор.
- Другому нужен чёткий план и правила.
- Третий — ловит язык на лету из мемов и сериалов.

🔴 Но чаще всего люди просто хватаются за первое попавшееся — и буксуют. Не потому что «не хватает мотивации», а потому что стиль не их.

Этот квиз — лёгкий способ за пару минут понять:
какой формат работает конкретно для тебя,
что тебя ускорит, а что, наоборот, мешает,
и с чего лучше начать, чтобы учёба наконец зашла.

✅ Начнём?

<b>Расскажи, что тебе обычно нравится в обучении?</b>

Каĸ ты обычно учишь английсĸий?
""".Trim(),
                new[] { "С курсов", "Самостоятельно", "С репетитором", "Поĸа ниĸаĸ" }),

            [QuizStep.Experience] = ("Что даётся тебе легче всего?", new[] { "Чтение", "Слушание", "Разговор", "Письмо" }),
            [QuizStep.Capital] = ("Что чаще всего выбивает из процесса?", new[] { "Сĸуĸа", "Страх ошибок", "Нет времени", "Не вижу прогресса" }),
            [QuizStep.IncomeSources] = ("Каĸ ты учишь новые слова?", new[] { "Зубрю списĸи", "Через сериалы, мемы", "В разговорах", "Не учу, просто встречаю" }),
            [QuizStep.SpareMoney] = ("Сĸольĸо времени ты реально можешь выделить в день?", new[] { "5–10 мин", "15-30 мин", "30+ мин", "Не знаю" }),
            [QuizStep.ExpenseTracking] = ("Что тебя больше всего мотивирует?", new[] { "Чётĸий план", "Игра и вызовы", " Разговорная праĸтиĸа", "Культура и погружение" }),
            [QuizStep.BudgetLeak] = ("Каĸ ты обычно справляешься с ошибĸами?", new[] { "Смущаюсь", "Разбираю и иду дальше", "Не замечаю", "Избегаю ситуаций" }),
            [QuizStep.Reserve] = ("«Где хочешь видеть свой прогресс через полгода?", new[] { "Разговор", "Работа", "Путешествия", "Просто для себя" }),
            [QuizStep.Goal] = ("Если честно: что тебе сейчас нужнее всего?", new[] { "Струĸтура", "Уверенность", "Регулярность", "Поддержка и комьюнити" }),
        };

        private readonly Dictionary<QuizStep, (string Q, string[] Opts)> _map;
        private readonly ChecklistService _checklist;
        private readonly BotDbContext _db;

        public QuizHandler(Dictionary<QuizStep, (string, string[])> map, ChecklistService checklist, BotDbContext db)
        {
            _map = map;
            _checklist = checklist;
            _db = db;
        }

        public bool CanHandle(Update u, UserState s) =>
            u.Message is { Type: MessageType.Text } &&
            s.Step is >= QuizStep.Role and < QuizStep.Finished;  // FIX

        public async Task HandleAsync(
            ITelegramBotClient bot,
            Update u,
            UserState state,
            StateService states,
            CancellationToken ct)
        {

            long chat = u.Message!.Chat.Id;
            var prevStep = state.Step;
            string answer = u.Message.Text!.Trim();

            // сравните ответ с вариантами, как вы уже делали …
            if (!_map[state.Step].Opts.Any(o => o.Trim().Equals(answer, StringComparison.OrdinalIgnoreCase)))
                return;  // не кнопка — игнор

            var rec = new AnswerRecord
            {
                ChatId = chat,
                Step = prevStep,
                Response = answer,
                AnsweredAt = DateTime.UtcNow
            };
            _db.Answers.Add(rec);
            await _db.SaveChangesAsync(ct);

            // сохраняем ответ и увеличиваем шаг
            state.Answers[prevStep] = answer;
            state.Step = Next(prevStep);

            // ── вот здесь сохраняем изменившийся state ─────────────────────────
            states.Save(chat, state);   // или Reset+Get, см. выше
                                        // ──────────────────────────────────────────────────────────────────

            // дальше идёт обработка Finished / отправка следующего вопроса…

            // ── если опрос окончен ───────────────────────────────────────────────
            if (state.Step == QuizStep.Finished)
            {
                var checklist = _checklist.Build(state.Answers);
                await bot.SendMessage(chat, checklist, parseMode: ParseMode.Html, cancellationToken: ct);

                var pdf1 = Path.Combine(AppContext.BaseDirectory, "Assets", "Checklist1.pdf");
                var pdf2 = Path.Combine(AppContext.BaseDirectory, "Assets", "Checklist2.pdf");
                await using var fs1 = File.OpenRead(pdf1);

                await bot.SendDocument(chat,
                    InputFile.FromStream(fs1, "Checklist1.pdf"),
                    "Ваш чек-лист (PDF)",
                    cancellationToken: ct);                            // FIX

                await using var fs2 = File.OpenRead(pdf1);

                await bot.SendDocument(chat,
                    InputFile.FromStream(fs2, "Checklist2.pdf"),
                    "Ваш чек-лист (PDF)",
                    cancellationToken: ct);

                // ── приглашение через 2 мин (можно вернуть задержку) ────────────
                _ = Task.Run(async () =>
                {
                    // await Task.Delay(TimeSpan.FromMinutes(2), ct);

                    const string invite = """
<b>🔥 Твое приглашение уже ждёт! Бесплатный мини-урок + консультация</b>

Теперь ты знаешь, какой у тебя стиль изучения английского и какие подходы работают лично для тебя. Следующий шаг — закрепить это в деле, поговорить с преподавателем и за 30 минут получить точный маршрут и крутые лайфхаки.

За полчаса мы:

1. <b>📌 Разберём твою точку А</b>
Посмотрим, где уже получается, а где затыки. Снимем тревожность и уберём ощущение «я не умею».
2. <b>🚀 Найдём твой стиль обучения</b>
Скажем, стоит ли тебе сразу погружаться в TikTok и мемы, или сначала чуть-чуть прокачать грамматику.
3. <b>🎯 Дадим 3 точных шага вперёд</b>
Без воды: как сделать язык ежедневной привычкой, а не ещё одним стрессом.

И, конечно, время выбрать максимально удобное:

<b>Вторник — «Мини-урок без стресса»</b>
Разберём базовые навыки: сленг, разговор, простые конструкции.

<b>Четверг — «Стратегия роста»</b>
Чёткий план под твои цели: подготовка к поездке, учёбе, работе.
""";

                    var kb = new InlineKeyboardMarkup(new[]
                    {
                        new [] { InlineKeyboardButton.WithCallbackData("Вторник — записаться на урок", "ticket_Tue") },
                        new [] { InlineKeyboardButton.WithCallbackData("Четверг — записаться на урок", "ticket_Thu") }
                    });

                    await bot.SendMessage(chat, invite,
                        parseMode: ParseMode.Html,                     // FIX
                        replyMarkup: kb, cancellationToken: ct);
                });

                states.Reset(chat);
                return;
            }

            // ── шлём следующий вопрос ───────────────────────────────────────────
            var (q, opts) = _map[state.Step];
            await bot.SendMessage(chat, q,                      // FIX
                parseMode: ParseMode.Html,
                replyMarkup: BuildReply(opts), cancellationToken: ct);
        }

        private static QuizStep Next(QuizStep step) =>
            step == QuizStep.Goal ? QuizStep.Finished : (QuizStep)((int)step + 1);

        private static ReplyMarkup BuildReply(string[] opts) =>
            opts.Length == 0
                ? new ReplyKeyboardRemove()
                : new ReplyKeyboardMarkup(opts.Select(o => new[] { new KeyboardButton(o) }))
                { ResizeKeyboard = true, OneTimeKeyboard = true };
    }
}
