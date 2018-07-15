using Nursery.Plugins;
using Nursery.Utility;

namespace Nursery.SoundEffectPlugin {
	public class SoundEffectReloadCommand : AbstractMentionKeywordCommand {
		public override string Name { get; } = "Nursery.SoundEffectPlugin.SoundEffectReloadCommand";
		// TRANSLATORS: Bot-Help message. SoundEffectReloadCommand plugin.
		public override string HelpText { get; } = T._("Reload sound list.") + " ```@voice-bot soundreload\n@voice-bot sereload```";

		private SoundEffectCommand _plg_se = null;
		private SoundEffectListCommand _plg_list = null;

		public override void Initialize(IPluginManager loader, IPlugin[] plugins) { }

		public SoundEffectReloadCommand() : base(new string[] { "soundreload", "sereload" }) { }

		private void LoadPlugin(IBot bot) {
			if (_plg_se == null) {
				_plg_se = bot.GetPlugin("Nursery.SoundEffectPlugin.SoundEffectCommand") as SoundEffectCommand;
				if (_plg_se == null) {
					// TRANSLATORS: Log message. SoundEffectReloadCommand plugin.
					Logger.Log(T._("Could not find SoundEffectCommand."));
				}
			}
			if (_plg_list == null) {
				_plg_list = bot.GetPlugin("Nursery.SoundEffectPlugin.SoundEffectListCommand") as SoundEffectListCommand;
				if (_plg_list == null) {
					// TRANSLATORS: Log message. SoundEffectReloadCommand plugin.
					Logger.Log(T._("Could not find SoundEffectListCommand."));
				}
			}
		}

		protected override bool DoExecute(int keywordIndex, IBot bot, IMessage message) {
			if (_plg_se == null || _plg_list == null) { LoadPlugin(bot); }
			message.Content = "";
			message.Terminated = true;
			message.AppliedPlugins.Add(this.Name);
			if (_plg_se == null || _plg_list == null) {
				// TRANSLATORS: Bot message. SoundEffectReloadCommand plugin. If it is longer than DISCORD_MESSAGE_MAX, it will be cut.
				bot.SendMessageAsync(message.Original.Channel, message.Original.Author, T._("Sorry, I could not get information."), true);
			} else {
				_plg_se.Reload();
				_plg_list.Reload(bot);
				// TRANSLATORS: Bot message. SoundEffectReloadCommand plugin. If it is longer than DISCORD_MESSAGE_MAX, it will be cut.
				bot.SendMessageAsync(message.Original.Channel, message.Original.Author, T._("Sound data reloaded."), true);
			}
			return true;
		}
	}
}
