using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MasloBot.Services
{
    internal class TelegramBotHostedService : IHostedService, IDisposable
    {
        private readonly ITelegramService _telegramService;
        private readonly IServiceProvider _serviceProvider;

        public TelegramBotHostedService(
            ITelegramService telegramService,
           IServiceProvider serviceProvider)
        {
            _telegramService = telegramService;
            _serviceProvider = serviceProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var persistence = scope.ServiceProvider.GetRequiredService<Persistence>();
                persistence.Database.EnsureCreated();
                if (!persistence.Offices.Any())
                {
                    persistence.Offices.Add(new Office { Name = "Офис на Ленина" });
                    persistence.Offices.Add(new Office { Name = "Офис в Искитиме" });
                    persistence.Offices.Add(new Office { Name = "Офис на вокзале" });
                    persistence.Offices.Add(new Office { Name = "Лесной офис" });
                    persistence.SaveChanges();
                }
            }
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _telegramService?.Dispose();
        }
    }
}
