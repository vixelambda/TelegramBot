using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Dadata;
using System.Collections.Concurrent;
using System.Text;
namespace TelegramBot;
public class BotService
{
    private readonly ITelegramBotClient botClient;
    private readonly SuggestClientAsync dadataClient;
    private readonly ConcurrentDictionary<long, string> lastActions = new();
    private readonly LinksConfig links;

    public BotService(ITelegramBotClient botClient, string dadataToken, LinksConfig links)
    {
        this.botClient = botClient;
        dadataClient = new SuggestClientAsync(dadataToken);
        this.links = links;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        botClient.StartReceiving(HandleUpdateAsync, HandleErrorAsync);
    }
    
    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken token)
    {
        if (update.Type != UpdateType.Message || update.Message!.Type != MessageType.Text)
            return;

        var message = update.Message;
        var chatId = message.Chat.Id;
        var text = message.Text!.Trim();

        LogMessage("ПОЛУЧЕНО", chatId, text);

        try
        {
            if (text.Equals("/start"))
            {
                await SendText(chatId, "Привет! Я бот для поиска компаний по введенному ИНН. Введите /help для отображения списка команд.");
            }
            else if (text.Equals("/help"))
            {
                await SendText(chatId, "/start – начать общение\n/help – справка о доступных командах\n/hello – информация обо мне\n/inn [ИНН] или\n/inn [ИНН1 ... ИННn] – поиск компаний по ИНН\n/last – повтор последнего действия");
            }
            else if (text.Equals("/hello"))
            {
                await SendText(chatId, "Фамилия, имя: Директоров Виктор\n" +
                    "Email: direktorov.v.d@gmail.com\n" +
                    $"GitHub: {links.LinkGithub}\n" +
                    $"Резюме на hh: {links.ResumeHH}");
            }
            else if (text.StartsWith("/inn"))
            {
                var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1).ToArray();
                if (parts.Length == 0)
                {
                    await SendText(chatId, "Укажите хотя бы один ИНН, например:\n/inn 7707083893\nили несколько ИНН, например:\n/inn 7707083893 7731457980");
                    return;
                }

                var results = new List<(string Inn, string Name, string Address)>();
                foreach (var inn in parts)
                {
                    try
                    {
                        var response = await dadataClient.FindParty(inn);
                        if (response.suggestions.Count > 0)
                        {
                            var party = response.suggestions[0].data;
                            results.Add((party.inn, party.name.full_with_opf, party.address.value));
                        }
                        else
                        {
                            results.Add(($"{inn}", null, null));
                        }
                    }
                    catch
                    {
                        await SendText(chatId, "Ошибка при запросе");
                    }
                }
                
                var sorted = results.OrderBy(r => r.Name).ToList();
                var sb = new StringBuilder("");
                foreach (var r in sorted)
                {
                    if (r.Name is not null) { sb.AppendLine($"✅️ ИНН: {r.Inn}\n🏢 {r.Name}\n📍 {r.Address}\n"); }
                    else { sb.AppendLine($"❌️ ИНН: {r.Inn}\n🏢 Компании с таким ИНН не существует\n"); }
                }

                await SendText(chatId, sb.ToString());
            }
            else if (text.Equals("/last"))
            {
                if (lastActions.TryGetValue(chatId, out var last))
                    await SendText(chatId, last);
                else
                    await SendText(chatId, "Нет предыдущего действия.");
            }
            else if (text.StartsWith("/"))
            {
                await SendText(chatId, "Неизвестная команда. Введите /help для отображения списка команд.");
            }
            else
            {
                await SendText(chatId, "Это не команда. Введите /help для отображения списка команд.");
            }
        }
        catch (Exception ex)
        {
            await SendText(chatId, $"Произошла ошибка: {ex.Message}");
        }
    }

    private Task HandleErrorAsync(ITelegramBotClient client, Exception exception, CancellationToken token)
    {
        Console.WriteLine($"Ошибка бота: {exception.Message}");
        return Task.CompletedTask;
    }

    private async Task SendText(long chatId, string text)
    {
        LogMessage("ОТПРАВЛЕНО", chatId, text);
        await botClient.SendTextMessageAsync(chatId, text);
        lastActions[chatId] = text;
    }

    private void LogMessage(string direction, long chatId, string text)
    {
        var timestamp = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");
        var header = $"[{timestamp}] [{direction}] [ChatId: {chatId}]";

        Console.ForegroundColor = direction == "ПОЛУЧЕНО" ? ConsoleColor.Cyan : ConsoleColor.Green;
        Console.WriteLine($"{header}");
        Console.ResetColor();
        Console.WriteLine(text);
        Console.WriteLine(new string('-', 100));
    }
}