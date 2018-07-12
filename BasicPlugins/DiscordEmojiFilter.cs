using System.Text.RegularExpressions;
using Nursery.Plugins;
using Nursery.Utility;

namespace Nursery.BasicPlugins {
	public class DiscordEmojiFilter : IPlugin {
		public string Name { get; } = "Nursery.BasicPlugins.DiscordEmojiFilter";
		// TRANSLATORS: Bot-Help message. DiscordEmojiFilter plugin.
		public string HelpText { get; } = T._("Filter to replace Discord-emoji from number format to :shortname: format.");
		Plugins.Type IPlugin.Type => Plugins.Type.Filter;

		private static Regex EMOJI = new Regex(@"(\s*)<:([^\:]+):([0-9]+)>(\s*)");

		public void Initialize(IPluginManager loader, IPlugin[] plugins) { }

		public bool Execute(IBot bot, IMessage message) {
			var m = message.Content;
			m = EMOJI.Replace(m, (Match match) => {
				if (match.Groups.Count != 5) { return ""; }
				// TRANSLATORS: DebugLog message. DiscordEmojiFilter plugin. {0} is Emoji name. {1} is Emoji Id.
				Logger.DebugLog(T._("* Discord emoji :{0}:{1} is replaced.", match.Groups[2].Value, match.Groups[3].Value));
				return match.Groups[1].Value + match.Groups[2].Value + match.Groups[4].Value;
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
