using Discord.WebSocket;
using System.Linq;

namespace Nursery.Plugins {
	public class JSArgument {
		public IBot Bot;
		public IMessage Message;
		public IUser Author;
		public string[] MentionedUsers;

		public JSArgument(IBot bot, IMessage message) {
			this.Bot = bot;
			this.Message = message;
			this.Author = new JSArgumentUser(message.Original.Author);
			this.MentionedUsers = message.Original.MentionedUsers.Select(mu => mu.Id.ToString()).ToArray();
		}
	}

	public class JSArgumentUser : IUser {
		public string Id { get; }
		public string Username { get; }
		public string Nickname { get; } = "";
		public string[] RoleIds { get; } = new string[] { };

		public JSArgumentUser(SocketUser user) {
			this.Id = user.Id.ToString();
			this.Username = user.Username;
			var u = user as SocketGuildUser;
			if (u == null) { return; }
			this.Nickname = u.Nickname;
			this.RoleIds = u.Roles.Select(r => r.Id.ToString()).ToArray();
		}
	}
}
