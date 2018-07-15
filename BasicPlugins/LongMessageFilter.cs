using Newtonsoft.Json;
using Nursery.Plugins;
using Nursery.Utility;

namespace Nursery.BasicPlugins {
	[JsonObject("Nursery.BasicPlugins.LongMessageFilter")]
	public class LongMessageFilterConfig {
		[JsonProperty("speedup_length")]
		public int SpeedUpLength { get; set; } = 100;
		[JsonProperty("speed")]
		public int Speed { get; set; } = 150;
		[JsonProperty("omit_length")]
		public int OmitLength { get; set; } = 100;
	}

	public class LongMessageFilter : IPlugin {
		public string Name { get; } = "Nursery.BasicPlugins.LongMessageFilter";
		// TRANSLATORS: Bot-Help message. LongMessageFilter plugin.
		public string HelpText { get; } = T._("Filter to shorten long messages.");
		Plugins.Type IPlugin.Type => Plugins.Type.Filter;

		private LongMessageFilterConfig config = null;

		public void Initialize(IPluginManager loader, IPlugin[] plugins) {
			try {
				this.config = loader.GetPluginSetting<LongMessageFilterConfig>(this.Name);
			} catch (System.Exception e) {
				Logger.DebugLog(e.ToString());
				this.config = null;
			}
			if (this.config == null) {
				this.config = new LongMessageFilterConfig();
			}
		}

		public bool Execute(IBot bot, IMessage message) {
			bool applied = false;
			if (this.config.SpeedUpLength > 0 && message.Content.Length > this.config.SpeedUpLength) {
				message.TalkOptions.Speed = this.config.Speed;
				applied = true;
			}
			if (this.config.OmitLength > 0 && message.Content.Length > this.config.OmitLength) {
				// TRANSLATORS: Bot message. LongMessageFilter plugin.
				message.Content = message.Content.Substring(0, this.config.OmitLength) + T._(" (omitted)");
				applied = true;
			}
			if (applied) {
				message.AppliedPlugins.Add(this.Name);
			}
			return applied;
		}
	}
}
