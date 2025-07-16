using System.Collections.Generic;
using EnglishMoodBot.State.Models;

namespace EnglishMoodBot.State
{
    /// <summary>
    /// In-memory-хранилище состояний пользователей.
    /// </summary>
    public sealed class StateService
    {
        private readonly Dictionary<long, UserState> _users = new();

        /// <summary>Получить состояние чата. Если его нет — создать пустое.</summary>
        public UserState Get(long chatId) =>
            _users.TryGetValue(chatId, out var s) ? s : (_users[chatId] = new());

        /// <summary>Сбросить состояние чата к дефолтному.</summary>
        public void Reset(long chatId) => _users[chatId] = new();

        /// <summary>Сохранить (обновить) состояние чата.</summary>
        public void Save(long chatId, UserState state) => _users[chatId] = state;   // ← добавлено
    }
}
