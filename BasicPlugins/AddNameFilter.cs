using Discord.WebSocket;
using Newtonsoft.Json;
using Nursery.Plugins;
using Nursery.Utility;

namespace Nursery.BasicPlugins {
	[JsonObject("Nursery.BasicPlugins.AddNameFilterConfig")]
	public class AddNameFilterConfig {
		[JsonProperty("ignore_prefix")]
		public string IgnorePrefix { get; set; } = "";
	}

	public class AddNameFilter : IPlugin {
		public const string NAME = "Nursery.BasicPlugins.AddNameFilter";
		public string Name { get; } = NAME;
		// TRANSLATORS: Bot-Help message. AddNameFilter plugin.
		public string HelpText { get; } = T._("Filter to add Nickname or Username to messages.");
		Plugins.Type IPlugin.Type => Plugins.Type.Filter;

		private AddNameFilterConfig config = null;

		public AddNameFilterConfig Config { get => config; }

		public void Initialize(IPluginManager loader, IPlugin[] plugins) {
			try {
				this.config = loader.GetPluginSetting<AddNameFilterConfig>(this.Name);
			} catch (System.Exception e) {
				Logger.DebugLog(e.ToString());
				this.config = null;
			}
			if (this.config == null) {
				this.config = new AddNameFilterConfig();
			}
		}

		public bool Execute(IBot bot, IMessage message) {
			if (message.Content.Length == 0) { return false; }
			var user = (message.Original.Channel as SocketGuildChannel).Guild.GetUser(message.Original.Author.Id);
			if (user == null) { return false;  }
			if (this.config.IgnorePrefix.Length > 0 && message.Content.IndexOf(this.config.IgnorePrefix) == 0) { return false; }
			if (user.Nickname == null || user.Nickname.Length == 0) {
				// TRANSLATORS: Bot message. AddNameFilter plugin. {0} is user name. {1} is message content.
				message.Content = T._("{0} says: {1}", user.Username, message.Content);
			} else {
				// TRANSLATORS: Bot message. AddNameFilter plugin. {0} is user name. {1} is message content.
				message.Content = T._("{0} says: {1}", user.Nickname, message.Content);
			}
			message.AppliedPlugins.Add(this.Name);
			return true;
		}
	}
}
