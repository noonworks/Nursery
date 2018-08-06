using Nursery.Plugins;

namespace Nursery.BasicPlugins {
	public class IgnoreSelfFilter : IPlugin {
		public string Name { get; } = "Nursery.BasicPlugins.IgnoreSelfFilter";
		// TRANSLATORS: Bot-Help message. IgnoreSelfFilter plugin.
		public string HelpText { get; } = T._("Filter to ignore voice-bot's self message.");
		Plugins.Type IPlugin.Type => Plugins.Type.Filter;

		public void Initialize(IPluginManager loader, IPlugin[] plugins) { }

		public bool Execute(IBot bot, IMessage message) {
			if (bot.Id == message.Original.Author.Id && !message.AppliedPlugins.Contains(AnnouncementFilter.NAME)) {
				message.Content = "";
				message.Terminated = true;
				message.AppliedPlugins.Add(this.Name);
				return true;
			}
			return false;
		}
	}
}
