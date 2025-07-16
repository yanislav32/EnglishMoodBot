using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace EnglishMoodBot.Data
{
    /// <summary>
    /// Используется инструментом dotnet ef для создания контекста в design-time.
    /// </summary>
    public class BotDbContextFactory : IDesignTimeDbContextFactory<BotDbContext>
    {
        public BotDbContext CreateDbContext(string[] args)
        {
            // Получаем текущее окружение (если не задано, считается Production)
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

            // Строим IConfiguration так же, как в Program.cs
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile($"appsettings.{env}.json", optional: true)
                .AddUserSecrets<BotDbContextFactory>(optional: true)
                .AddEnvironmentVariables()
                .Build();

            // Берём строку подключения из Configuration
            var conn = config.GetConnectionString("BotDb")
                       ?? throw new InvalidOperationException(
                           "ConnectionStrings:BotDb missing in configuration");

            // Настраиваем DbContextOptions
            var options = new DbContextOptionsBuilder<BotDbContext>()
                .UseNpgsql(conn)
                .Options;

            return new BotDbContext(options);
        }
    }
}
