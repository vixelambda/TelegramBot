using Microsoft.Extensions.Configuration;
using Telegram.Bot;
namespace TelegramBot;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("links.json", optional: false, reloadOnChange: true);


        var configuration = builder.Build();

        var botToken = configuration["TelegramBotToken"];
        var dadataToken = configuration["DadataToken"];
        var linksConfig = configuration.Get<LinksConfig>();

        var botClient = new TelegramBotClient(botToken);
        var botService = new BotService(botClient, dadataToken, linksConfig);

        var cts = new CancellationTokenSource();

        await botService.StartAsync(cts.Token);

        Console.WriteLine("Бот запущен. Нажмите Сtrl+X для завершения.");
        Console.WriteLine(new string('-', 100));

        while (true)
        {
            var keyInfo = Console.ReadKey(true);
            if (keyInfo.Key == ConsoleKey.X && keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control))
            {
                Console.WriteLine("Получен Ctrl+X. Завершение работы бота...");
                break;
            }
        }

        cts.Cancel();
        Console.WriteLine("Завершение работы...");
    }
}