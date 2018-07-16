using Discord.WebSocket;
using System.Collections.Generic;
using System.Linq;

namespace Nursery {
	class BotState {
		public bool Joined { get; set; } = false;
		public List<ulong> TextChannelIds { get; set; } = new List<ulong>();
		public ulong VoiceChannelId { get; set; } = 0;
		public SocketGuild Guild { get; private set; } = null;
		public string Nickname { get; set; } = "";
		public ulong[] RoleIds { get; set; } = new ulong[] { };

		public void SetGuild(SocketGuild guild, ulong BotId) {
			this.Guild = guild;
			this.Nickname = "";
			this.RoleIds = new ulong[] { };
			if (guild != null) {
				var cu = guild.GetUser(BotId);
				if (cu != null) {
					this.Nickname = cu.Nickname;
					this.RoleIds = cu.Roles.Select(r => r.Id).ToArray();
				}
			}
		}
	}
}
