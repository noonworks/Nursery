using Discord.WebSocket;
using System.Text.RegularExpressions;
using Nursery.Plugins;
using Nursery.Utility;

namespace Nursery.BasicPlugins {
	public class MentionFilter : IPlugin {
		public string Name { get; } = "Nursery.BasicPlugins.MentionFilter";
		// TRANSLATORS: Bot-Help message. MentionFilter plugin.
		public string HelpText { get; } = T._("Filter to replace mentions from number format to Nickname or Username.");
		Plugins.Type IPlugin.Type => Plugins.Type.Filter;

		private static Regex EVERYONE = new Regex(@"^(.*\s+)?@everyone(\s+.*)?$");
		private static Regex HERE = new Regex(@"^(.*\s+)?@here(\s+.*)?$");
		private static Regex MENTION = new Regex(@"(\s*)<@!?([0-9]+)>(\s*)");

		public void Initialize(IPluginManager loader, IPlugin[] plugins) { }

		public bool Execute(IBot bot, IMessage message) {
			var guild = (message.Original.Channel as SocketGuildChannel).Guild;
			var m = message.Content;
			// TRANSLATORS: Bot message. MentionFilter plugin. Replacer for "@everyone".
			m = EVERYONE.Replace(m, "$1" + T._(" at everyone ") + "$2");
			// TRANSLATORS: Bot message. MentionFilter plugin. Replacer for "@here".
			m = HERE.Replace(m, "$1" + T._(" at here ") + "$2");
			m = MENTION.Replace(m, (Match match) => {
				if (match.Groups.Count != 4) { return ""; }
				ulong id;
				if (!ulong.TryParse(match.Groups[2].Value, out id)) { return match.Groups[1].Value + match.Groups[3].Value; }
				var user = guild.GetUser(id);
				if (user == null) { return match.Groups[1].Value + match.Groups[3].Value; }
				var name = user.Nickname;
				if (name == null || name.Length == 0) { name = user.Username; }
				// TRANSLATORS: Bot message. MentionFilter plugin. Replacer for "@XXX". {0} is user name. 
				return match.Groups[1].Value + T._("at {0}", name) + match.Groups[3].Value;
			});
			if (!m.Equals(message.Content)) {
				message.Content = m;
				message.AppliedPlugins.Add(this.Name);
				return true;
			}
			return false;
		}
	}
}
