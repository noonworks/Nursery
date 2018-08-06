using Nursery.Plugins;
using System.Linq;

namespace Nursery.BasicPlugins {
	public class ChannelFilter : IPlugin {
		public string Name { get; } = "Nursery.BasicPlugins.ChannelFilter";
		// TRANSLATORS: Bot-Help message. ChannelFilter plugin.
		public string HelpText { get; } = T._("Filter to ignore messages from the text channel where voice-bot is not joined.");
		Plugins.Type IPlugin.Type => Plugins.Type.Filter;

		public void Initialize(IPluginManager loader, IPlugin[] plugins) { }

		public bool Execute(IBot bot, IMessage message) {
			if (!bot.TextChannelIds.Contains(message.Original.Channel.Id)) {
				message.Content = "";
				message.Terminated = true;
				message.AppliedPlugins.Add(this.Name);
				return true;
			}
			return false;
		}
	}
}
