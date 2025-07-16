using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EnglishMoodBot.Data
{
    /// <summary>
    /// Используется инструментом dotnet ef для создания контекста в design-time.
    /// </summary>
    public class BotDbContextFactory : IDesignTimeDbContextFactory<BotDbContext>
    {
        public BotDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<BotDbContext>();

            // жёстко вписываем тут строку подключения OR читаем из ENV:
            // var conn = Environment.GetEnvironmentVariable("BOTDB__ConnectionStrings__BotDb")
            var conn = "Host=localhost;Port=5432;Database=botdb;Username=botuser;Password=Alan98325";

            builder.UseNpgsql(conn);

            return new BotDbContext(builder.Options);
        }
    }
}
