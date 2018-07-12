using Nursery.Plugins;

namespace Nursery.BasicPlugins {
	public class JoinedFilter : IPlugin {
		public string Name { get; } = "Nursery.BasicPlugins.JoinedFilter";
		// TRANSLATORS: Bot-Help message. JoinedFilter plugin.
		public string HelpText { get; } = T._("Filter to ignore messages when voice-bot is not joining any channel.");
		Plugins.Type IPlugin.Type => Plugins.Type.Filter;

		public void Initialize(IPluginManager loader, IPlugin[] plugins) { }

		public bool Execute(IBot bot, IMessage message) {
			if (! bot.IsJoined) {
				message.Content = "";
				message.Terminated = true;
				message.AppliedPlugins.Add(this.Name);
				return true;
			}
			return false;
		}
	}
}
