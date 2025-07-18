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
            [QuizStep.Capital] = ("Что чаще всего выбивает из процесса?", new[] { "< Сĸуĸа", "Страх ошибок", "Нет времени", "Не вижу прогресса" }),
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

                var pdf = Path.Combine(AppContext.BaseDirectory, "Assets", "Checklist.pdf");
                await using var fs = File.OpenRead(pdf);

                await bot.SendDocument(chat,
                    InputFile.FromStream(fs, "Checklist.pdf"),
                    "Ваш чек-лист (PDF)",
                    cancellationToken: ct);                            // FIX

                // ── приглашение через 2 мин (можно вернуть задержку) ────────────
                _ = Task.Run(async () =>
                {
                    // await Task.Delay(TimeSpan.FromMinutes(2), ct);

                    const string invite = """
<b>🎟 Мы оставили для вас подарочный e-билет — заберите, пока место свободно</b>

Вы уже знаете, куда течёт ваш капитал и где он недорабатывает.
Следующий шаг — окружить себя людьми, которые решают те же задачи и делятся готовыми ходами.

<i><u>Поэтому мы дарим личный электронный билет на встречу клуба «Советника»: мероприятие, чтобы за два часа:</u></i>

<b>1. Усилить +Доход</b>
Наш аналитик покажет, как найти «тихие» 6-8 % годовых там, где раньше видели лишь банковский процент.
<b>2. Сократить –Потери</b>
Разберём реальные кейсы гостей: комиссии, бесполезные подписки, вялые активы. Уйдёте с чек-листом экономии в руках.
<b>3. Построить =Стабильность</b>
Пошагово соберём резерв «6 × 6»: шесть месяцев спокойствия, шесть вариантов ликвидности.

<blockquote>Нетворкинг: кофе, лёгкий фуршет, 20-25 участников, которые говорят о деньгах так же свободно, как о путешествиях.</blockquote>

Когда удобно встретиться?

Вторник  — «Рынок без шума»: свежая аналитика + Q&A
Четверг  — «Личные стратегии»: мини-коучинг под ваши цели
""";

                    var kb = new InlineKeyboardMarkup(new[]
                    {
                        new [] { InlineKeyboardButton.WithCallbackData("Вторник — получить билет", "ticket_Tue") },
                        new [] { InlineKeyboardButton.WithCallbackData("Четверг — получить билет", "ticket_Thu") }
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
