using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace MasloBot.Services
{
    interface ITelegramService: IDisposable
    {
        Task SendMessage(long chatId, string text);
    }

    internal class TelegramService : ITelegramService
    {
        private readonly TelegramBotClient _telegramClient;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TelegramService> _logger;

        public TelegramService(
            IServiceProvider serviceProvider,
            Constants constants,
            ILogger<TelegramService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;

            _telegramClient = CreateClient(constants);
            _telegramClient.StartReceiving(new[] { UpdateType.Message, UpdateType.CallbackQuery });
            _telegramClient.OnMessage += OnMessage;
            _telegramClient.OnCallbackQuery += OnCallbackQuery;
        }

        public async Task SendMessage(long chatId, string text)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var persistence = scope.ServiceProvider.GetRequiredService<Persistence>();
                await persistence.AddSentMessage(chatId, text);
            }
            await _telegramClient.SendTextMessageAsync(chatId, text);
        }

        public void Dispose()
        {
            _telegramClient.OnCallbackQuery -= OnCallbackQuery;
            _telegramClient.OnMessage -= OnMessage;
        }

        private TelegramBotClient CreateClient(Constants constants)
        {
            if (!string.IsNullOrWhiteSpace(constants.BotProxy))
            {
                _logger.LogDebug("Telegram client proxy {proxy}", constants.BotProxy);

                var proxyString = constants.BotProxy.Split(':');
                var proxyHost = proxyString[0];
                var proxyPort = int.Parse(proxyString[1]);

                var proxy = new MihaZupan.HttpToSocks5Proxy(proxyHost, proxyPort);
                proxy.ResolveHostnamesLocally = true; 
                
                return new TelegramBotClient(constants.BotToken, proxy);
            }

            return new TelegramBotClient(constants.BotToken);
        }

        private async void OnCallbackQuery(object sender, CallbackQueryEventArgs e)
        {
            var message = e.CallbackQuery.Message;
            var chat = message.Chat;

            var parts = e.CallbackQuery.Data.Split('_');

            if (parts.Length != 2)
                return;

            if (parts[0] == "Office")
            {
                var officeId = long.Parse(parts[1]);
                using (var scope = _serviceProvider.CreateScope())
                {
                    var persistence = scope.ServiceProvider.GetRequiredService<Persistence>();
                    var office = await persistence.SetUserToOffice(chat.Id, officeId);
                    await _telegramClient.SendTextMessageAsync(chat.Id, "Вы добавлены в чат: " + office.Name);
                }
            }

            await _telegramClient.DeleteMessageAsync(chat.Id, message.MessageId);
        }

        private async void OnMessage(object sender, MessageEventArgs e)
        {
            var text = e.Message?.Text;
            var chat = e.Message?.Chat;

            using (var scope = _serviceProvider.CreateScope())
            {
                var persistence = scope.ServiceProvider.GetRequiredService<Persistence>();
                if (text == "/start")
                {
                    _ = await persistence.GetOrAddUser(chat.Id, $"{chat.FirstName} {chat.LastName}");

                    var buttons = persistence.Offices
                        .Select(x => new InlineKeyboardButton
                        {
                            Text = x.Name,
                            CallbackData = "Office_" + x.Id
                        })
                        .Select(x => new[] { x });

                    var keyboard = new InlineKeyboardMarkup(buttons);
                    await _telegramClient.SendTextMessageAsync(chat.Id, "Укажите, где вы:", replyMarkup: keyboard);
                }
                else if (text == "/chatid")
                {
                    await _telegramClient.SendTextMessageAsync(chat.Id, $"ChatId: {chat.Id}");
                }
                else
                {
                    var user = await persistence.GetOrAddUser(chat.Id, $"{chat.FirstName} {chat.LastName}");
                    await persistence.AddMessage(user, text, MessageType.In);
                }
            }
        }
    }
}
