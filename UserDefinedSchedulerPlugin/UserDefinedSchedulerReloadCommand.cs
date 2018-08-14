using Nursery.Plugins;
using Nursery.Utility;

namespace Nursery.UserDefinedSchedulerPlugin {
	public class UserDefinedSchedulerReloadCommand : AbstractMentionKeywordCommand {
		public override string Name { get; } = "Nursery.UserDefinedSchedulerPlugin.UserDefinedSchedulerReloadCommand";
		// TRANSLATORS: Bot-Help message. UserDefinedSchedulerReloadCommand plugin.
		public override string HelpText { get; } = T._("Reload user-defined schedules.") + " ```@voice-bot udsreload```";

		private UserDefinedScheduler _plg_uds = null;

		public override void Initialize(IPluginManager loader, IPlugin[] plugins) { }

		public UserDefinedSchedulerReloadCommand() : base(new string[] { "udsreload" }) { }

		private void LoadPlugin(IBot bot) {
			if (_plg_uds == null) {
				_plg_uds = bot.GetPlugin(UserDefinedScheduler.NAME) as UserDefinedScheduler;
				if (_plg_uds == null) {
					// TRANSLATORS: Log message. UserDefinedSchedulerReloadCommand plugin.
					Logger.Log(T._("Could not find UserDefinedScheduler."));
				}
			}
		}

		protected override bool DoExecute(int keywordIndex, IBot bot, IMessage message) {
			if (_plg_uds == null) { LoadPlugin(bot); }
			message.Content = "";
			message.Terminated = true;
			message.AppliedPlugins.Add(this.Name);
			if (_plg_uds == null) {
				// TRANSLATORS: Bot message. UserDefinedSchedulerReloadCommand plugin. If it is longer than DISCORD_MESSAGE_MAX, it will be cut.
				bot.SendMessageAsync(message.Original.Channel, message.Original.Author, T._("Sorry, I could not get information."), true);
			} else {
				_plg_uds.Reload(bot);
				// TRANSLATORS: Bot message. UserDefinedSchedulerReloadCommand plugin. If it is longer than DISCORD_MESSAGE_MAX, it will be cut.
				bot.SendMessageAsync(message.Original.Channel, message.Original.Author, T._("User-defined schedules are reloaded."), true);
			}
			return true;
		}
	}
}
