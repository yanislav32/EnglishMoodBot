namespace EnglishMoodBot.State.Models
{
    public class AnswerRecord
    {
        public int Id { get; set; }
        public long ChatId { get; set; }
        public QuizStep Step { get; set; }
        public string Response { get; set; } = "";
        public DateTime AnsweredAt { get; set; }

        public UserRecord User { get; set; } = null!;
    }
}
