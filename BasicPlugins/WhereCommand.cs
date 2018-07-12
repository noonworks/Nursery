using Discord.WebSocket;
using Nursery.Plugins;

namespace Nursery.BasicPlugins {
	public class WhereCommand : AbstractMentionKeywordCommand {
		public override string Name { get; } = "Nursery.BasicPlugins.WhereCommand";
		// TRANSLATORS: Bot-Help message. WhereCommand plugin.
		public override string HelpText { get; } = T._("Show where voice-bot is in.") + " ```@voice-bot where```";

		public WhereCommand() : base(new string[] { "where" }) { }

		public override void Initialize(IPluginManager loader, IPlugin[] plugins) { }

		protected override bool DoExecute(int keywordIndex, IBot bot, IMessage message) {
			message.Content = "";
			message.Terminated = true;
			message.AppliedPlugins.Add(this.Name);
			var user = (message.Original.Author as SocketGuildUser);
			if (user == null || user.Guild == null) {
				// TRANSLATORS: Bot message. WhereCommand plugin. {0} is mention part.
				bot.SendMessageAsync(message.Original.Channel, T._("{0}Sorry, I can not find your server.", message.Original.Author.Mention + " "));
				return true;
			}
			// TRANSLATORS: Bot message. WhereCommand plugin.
			var m = message.Original.Author.Mention + "\n" + T._("Where am I:") + "\n";
			var vc = user.Guild.GetVoiceChannel(bot.VoiceChannelId);
			if (vc == null) {
				// TRANSLATORS: Bot message. WhereCommand plugin.
				m += T._("[I'm not joining any voice channel.]") + "\n";
			} else {
				// TRANSLATORS: Bot message. WhereCommand plugin.
				m += T._("[Voice channel]") + "\n" + vc.Name + "\n";
			}
			if (bot.TextChannelIds.Count == 0) {
				// TRANSLATORS: Bot message. WhereCommand plugin.
				m += T._("[I'm not joining any text channel.]") + "\n";
			} else {
				// TRANSLATORS: Bot message. WhereCommand plugin.
				m += T._n("[Text Channel]", "[Text Channels]", bot.TextChannelIds.Count) + "\n";
				foreach (var tcid in bot.TextChannelIds) {
					var tc = user.Guild.GetTextChannel(tcid);
					m += (tc == null ? "" : tc.Name + "\n");
				}
			}
			bot.SendMessageAsync(message.Original.Channel, m);
			return true;
		}
	}
}
