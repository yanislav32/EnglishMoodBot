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
<b>📊 Разобрались. Вот, что показывает ваша финансовая картина:</b>

— Доход задействован не полностью — есть потенциал усилить оборот.
— В тратах заметны пробелы: часть утекает незаметно.
— Резерв не покрывает и половины возможных рисков.

Это не критично. Но требует системной настройки.
Не глобальной реформы — а нескольких точечных шагов.

<b>📥 Мы уже собрали их для вас.</b>

Ниже — чек-лист “+ Доход / – Потери / = Стабильность”.
Чёткие ориентиры: что стоит пересмотреть и как стабилизировать систему уже в ближайший месяц.
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
