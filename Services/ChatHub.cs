using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace MasloBot.Services
{
    public class ChatHub : Hub
    {
        public async Task StateChanged()
        {
            await Clients.All.SendAsync("StateChanged");
        }
    }
}
