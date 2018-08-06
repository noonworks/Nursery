using Discord.WebSocket;
using System.Collections.Generic;
using System.Linq;

namespace Nursery {
	class BotState {
		private SocketVoiceChannel _VoiceChannel= null;
		private List<ISocketMessageChannel> _TextChannels = new List<ISocketMessageChannel>();

		public bool Joined { get => this._VoiceChannel != null; }
		public ulong VoiceChannelId { get => this._VoiceChannel == null ? 0 : this._VoiceChannel.Id; }
		public ulong[] TextChannelIds { get => _TextChannels.Select(tc => tc.Id).ToArray(); }
		public ISocketMessageChannel DefaultTextChannel {
			get {
				if (this._TextChannels.Count > 0) {
					return _TextChannels[0];
				}
				return null;
			}
		}
		public SocketGuild Guild { get; private set; } = null;
		public string Nickname { get; private set; } = "";
		public ulong[] RoleIds { get; private set; } = new ulong[] { };

		public bool Join(ISocketMessageChannel TextChannel, SocketVoiceChannel VoiceChannel, SocketGuild Guild, ulong BotId) {
			if (this.Joined) { return false; }
			this.AddTextChannel(TextChannel);
			this._VoiceChannel = VoiceChannel;
			this.SetGuild(Guild, BotId);
			return true;
		}

		public bool Leave() {
			if (!this.Joined) { return false; }
			this._TextChannels.Clear();
			this._VoiceChannel = null;
			this.SetGuild(null, 0);
			return true;
		}

		public bool AddTextChannel(ISocketMessageChannel channel) {
			if (this._TextChannels.Contains(channel)) {
				return false;
			}
			this._TextChannels.Add(channel);
			return true;
		}

		public bool RemoveTextChannel(ISocketMessageChannel channel) {
			if (this._TextChannels.Contains(channel)) {
				this._TextChannels.Remove(channel);
				return true;
			}
			return false;
		}

		private void SetGuild(SocketGuild guild, ulong BotId) {
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
