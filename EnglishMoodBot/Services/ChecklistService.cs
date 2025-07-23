using System.Text.RegularExpressions;
using EnglishMoodBot.State.Models;

namespace EnglishMoodBot.Services
{
    public sealed class ChecklistService
    {
        /// <summary>Строит перечень сообщений; каждое ≤ 4096 симв.</summary>
        public string Build(IReadOnlyDictionary<QuizStep, string> a)
        {
            // ↓ ваш длинный исходный текст (можно сделать const string в Resources)
            const string raw = """
<b>✅ Here’s your vibe — вот твой результат</b>

С твоими ответами всё ясно: вопрос не «способен ли я выучить язык», а <b>как сделать, чтобы учёба работала на тебя, а не наоборот.</b>

У каждого свой триггер прогресса: кому-то нужна чёткая рамка, кому-то — игра, кому-то — тёплый разговорный поток. Твой стиль уже проявился — теперь его надо подкрепить системой, а не стихийными «забегами по субботам».

Что дальше?

Мы приготовили для тебя Printable-календарь «365 слов»

• Вся сетка года на одной странице и рядом — список слов-выручалочек.
• Выучил(а) слово — зачеркнул(а) день.
• Смотришь, как словарь растёт, а привычка закрепляется — без гонки и зубрёжки.

Распечатай, повесь на стену или сохрани в телефоне, вычеркивай выученное — и смотри, как растёт словарь без перегруза и отсрочек «с понедельника». Главное: одно слово в день, один чек-марк. Простая механика, которая превращает английский из «надо бы» в ежедневное движение вперёд.

<b>Открывай календарь, отмечай первый день — и погнали. 🚀</b>
""";

            return raw;
        }

        // ——— helpers ———
        private static IEnumerable<string> SplitSafe(string text, int limit)
        {
            if (text.Length <= limit) { yield return text; yield break; }

            var words = text.Split(' ');
            var sb = new List<string>();
            var len = 0;

            foreach (var w in words)
            {
                if (len + w.Length + 1 > limit)
                {
                    yield return string.Join(' ', sb);
                    sb.Clear(); len = 0;
                }
                sb.Add(w);
                len += w.Length + 1;
            }
            if (sb.Count > 0) yield return string.Join(' ', sb);
        }
    }
}
