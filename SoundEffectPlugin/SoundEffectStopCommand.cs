using Nursery.Plugins;
using Nursery.Utility;

namespace Nursery.SoundEffectPlugin {
	public class SoundEffectStopCommand : AbstractMentionKeywordCommand {
		public override string Name { get; } = "Nursery.SoundEffectPlugin.SoundEffectStopCommand";
		// TRANSLATORS: Bot-Help message. SoundEffectStopCommand plugin.
		public override string HelpText { get; } = T._("Stop all sounds.") + " ```@voice-bot soundstop\n@voice-bot sestop```";

		private SoundEffectCommand _plg = null;

		public override void Initialize(IPluginManager loader, IPlugin[] plugins) { }

		public SoundEffectStopCommand() : base(new string[] { "soundstop", "sestop" }) { }

		private void LoadPlugin(IBot bot) {
			_plg = bot.GetPlugin("Nursery.SoundEffectPlugin.SoundEffectCommand") as SoundEffectCommand;
			if (_plg == null) {
				// TRANSLATORS: Log message. SoundEffectStopCommand plugin.
				Logger.Log(T._("Could not find SoundEffectCommand."));
				return;
			}
		}

		protected override bool DoExecute(int keywordIndex, IBot bot, IMessage message) {
			if (_plg == null) { LoadPlugin(bot); }
			if (_plg == null) {
				// TRANSLATORS: Bot message. SoundEffectStopCommand plugin.
				bot.SendMessageAsync(message.Original.Channel, message.Original.Author.Mention + " " + T._("Sorry, I could not get information."));
			} else {
				_plg.Stop();
				// TRANSLATORS: Bot message. SoundEffectStopCommand plugin.
				bot.SendMessageAsync(message.Original.Channel, message.Original.Author.Mention + " " + T._("All sounds are stopped."));
			}
			message.Content = "";
			message.Terminated = true;
			message.AppliedPlugins.Add(this.Name);
			return true;
		}
	}
}
