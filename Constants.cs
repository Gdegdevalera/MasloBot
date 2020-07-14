using Microsoft.Extensions.Configuration;

namespace MasloBot
{
    public class Constants
    {
        public string BotToken { get; }
        public string BotProxy { get; }

        public Constants(IConfiguration configuration)
        {
            BotToken = configuration.GetValue<string>("TelegramBot:Token");
            BotProxy = configuration.GetValue<string>("TelegramBot:Proxy");
        }
    }
}
