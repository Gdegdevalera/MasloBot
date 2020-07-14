using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace MasloBot.Services
{
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long Id { get; set; } // chat id

        public string Name { get; set; }

        public long? OfficeId { get; set; }

        public virtual Office Office { get; set; }
    }

    public class Office
    {
        [Key]
        public long Id { get; set; }

        public string Name { get; set; }
    }

    public class Message
    {
        [Key]
        public long Id { get; set; }

        public DateTime Date { get; set; }

        public string Text { get; set; }

        public MessageType Type { get; set; }

        public long? UserId { get; set; }

        public virtual User User { get; set; }

        public long? OfficeId { get; set; }

        public virtual Office Office { get; set; }
    }

    public enum MessageType
    {
        Unknown = 0,
        In,
        Out,
        Service
    }

    public class Chat
    {
        public long? ChatId { get; set; }

        public string Name { get; set; }

        public List<Message> Messages { get; set; }

        public List<User> Users { get; set; }

        public string UserNames { get { return string.Join(", ", Users.Select(x => x.Name)); } }
    }
}
