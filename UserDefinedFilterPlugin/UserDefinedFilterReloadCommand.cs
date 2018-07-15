using Nursery.Plugins;
using Nursery.Utility;

namespace Nursery.UserDefinedFilterPlugin {
	public class UserDefinedFilterReloadCommand : AbstractMentionKeywordCommand {
		public override string Name { get; } = "Nursery.UserDefinedFilterPlugin.UserDefinedFilterReloadCommand";
		// TRANSLATORS: Bot-Help message. UserDefinedFilterReloadCommand plugin.
		public override string HelpText { get; } = T._("Reload user-defined filters.") + " ```@voice-bot udfreload```";

		private UserDefinedFilter _plg_udf = null;

		public override void Initialize(IPluginManager loader, IPlugin[] plugins) { }

		public UserDefinedFilterReloadCommand() : base(new string[] { "udfreload" }) { }

		private void LoadPlugin(IBot bot) {
			if (_plg_udf == null) {
				_plg_udf = bot.GetPlugin("Nursery.UserDefinedFilterPlugin.UserDefinedFilter") as UserDefinedFilter;
				if (_plg_udf == null) {
					// TRANSLATORS: Log message. UserDefinedFilterReloadCommand plugin.
					Logger.Log(T._("Could not find UserDefinedFilter."));
				}
			}
		}

		protected override bool DoExecute(int keywordIndex, IBot bot, IMessage message) {
			if (_plg_udf == null) { LoadPlugin(bot); }
			message.Content = "";
			message.Terminated = true;
			message.AppliedPlugins.Add(this.Name);
			if (_plg_udf == null) {
				// TRANSLATORS: Bot message. UserDefinedFilterReloadCommand plugin. If it is longer than DISCORD_MESSAGE_MAX, it will be cut.
				bot.SendMessageAsync(message.Original.Channel, message.Original.Author, T._("Sorry, I could not get information."), true);
			} else {
				_plg_udf.Reload();
				// TRANSLATORS: Bot message. UserDefinedFilterReloadCommand plugin. If it is longer than DISCORD_MESSAGE_MAX, it will be cut.
				bot.SendMessageAsync(message.Original.Channel, message.Original.Author, T._("User-defined filters are reloaded."), true);
			}
			return true;
		}
	}
}
