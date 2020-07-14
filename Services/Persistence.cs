using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MasloBot.Services
{
    public class Persistence : DbContext
    {
        private readonly IHubContext<ChatHub> _hub;

        public Persistence(
            DbContextOptions<Persistence> options,
            IHubContext<ChatHub> hub) : base(options)
        {
            _hub = hub;
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Office> Offices { get; set; }

        public List<Chat> GetChats()
        {
            var chatToUsers = Users
                .AsNoTracking()
                .AsEnumerable()
                .GroupBy(x => x.OfficeId ?? 0)
                .ToDictionary(x => x.Key, x => x.ToList());

            var chatToMessages = Messages
                .Include(x => x.User)
                .AsNoTracking()
                .AsEnumerable()
                .GroupBy(x => x.OfficeId ?? 0)
                .ToDictionary(x => x.Key, x => x.ToList());

            var chats = Offices
                .AsNoTracking()
                .AsEnumerable()
                .Select(x => new Chat
                {
                    ChatId = x.Id,
                    Name = x.Name,
                    Messages = chatToMessages.SafeGetValue(x.Id) ?? new List<Message>(),
                    Users = chatToUsers.SafeGetValue(x.Id) ?? new List<User>()
                })
                .ToList();

            chats.Add(new Chat
            {
                ChatId = 0,
                Name = "Вне офиса",
                Messages = chatToMessages.SafeGetValue(0) ?? new List<Message>(),
                Users = chatToUsers.SafeGetValue(0) ?? new List<User>()
            });

            return chats;
        }

        public async Task<User> GetOrAddUser(long id, string name)
        {
            return Users.AsNoTracking().FirstOrDefault(x => x.Id == id) ?? await AddUser(id, name);            
        }

        public async Task<User> AddUser(long id, string name)
        {
            var user = new User { Id = id, Name = name };
            Users.Add(user);
            SaveChanges();

            await NotifyStateChanged();
            return user;
        }

        public async Task<Office> SetUserToOffice(long userId, long officeId)
        {
            var office = Offices.FirstOrDefault(x => x.Id == officeId);
            var user = Users.FirstOrDefault(x => x.Id == userId);

            Messages.Add(new Message {
                Text = $"{user.Name} покинул чат",
                OfficeId = user.OfficeId,
                Date = DateTime.Now,
                Type = MessageType.Service
            });

            user.OfficeId = office.Id;

            Messages.Add(new Message
            {
                Text = $"{user.Name} вошел в чат",
                OfficeId = user.OfficeId,
                Date = DateTime.Now,
                Type = MessageType.Service
            });

            SaveChanges();

            await NotifyStateChanged();
            return office;
        }

        public Task AddSentMessage(long userId, string text)
        {
            var user = Users.First(x => x.Id == userId);
            return AddMessage(user, text, MessageType.Out);
        }

        public async Task AddMessage(User user, string text, MessageType type)
        {
            var message = new Message
            {
                Date = DateTime.Now,
                Text = text,
                Type = type,
                UserId = user.Id,
                OfficeId = user.OfficeId
            };

            Messages.Add(message);
            SaveChanges();

            await NotifyStateChanged();
        }

        private Task NotifyStateChanged()
        {
            return _hub.Clients.All.SendAsync("StateChanged");
        }
    }
}
