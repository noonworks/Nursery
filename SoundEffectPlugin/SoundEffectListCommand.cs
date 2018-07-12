using System;
using System.Linq;
using Nursery.Plugins;
using Nursery.Utility;

namespace Nursery.SoundEffectPlugin {
	public class SoundEffectListCommand : AbstractMentionKeywordCommand {
		public override string Name { get; } = "Nursery.SoundEffectPlugin.SoundEffectListCommand";
		// TRANSLATORS: Bot-Help message. SoundEffectListCommand plugin.
		public override string HelpText { get; } = T._("Show sound list.") + " ```@voice-bot soundlist\n@voice-bot selist```";

		private string text = "";

		public SoundEffectListCommand() : base(new string[] { "soundlist", "selist" }) { }

		public void Reload(IBot bot) {
			var p = bot.GetPlugin("Nursery.SoundEffectPlugin.SoundEffectCommand") as SoundEffectCommand;
			if (p == null) {
				// TRANSLATORS: Log message. SoundEffectListCommand plugin.
				Logger.Log(T._("Could not find SoundEffectCommand."));
				return;
			}
			var sound_configs = p.SoundConfigs;
			// TRANSLATORS: Bot message. SoundEffectListCommand plugin.
			this.text = "\n\n" + T._("[`se NAME` or `sound NAME`]") + "\n";
			this.text += String.Join(", ", sound_configs.Select(s_conf => String.Join(", ", s_conf.Aliases)).Where(s => s.Length > 0).ToArray()) + "\n";
			// TRANSLATORS: Bot message. SoundEffectListCommand plugin.
			this.text += "\n" + T._("[Other keywords]") + "\n";
			this.text += String.Join("\n", sound_configs.Select(s_conf => {
				return String.Join("\n", s_conf.Patterns.Select(p_conf => {
					switch (p_conf.Type) {
						case PatternType.String:
							return "* " + p_conf.StrPattern;
						case PatternType.Regex:
							// TRANSLATORS: Bot message. SoundEffectListCommand plugin.
							return T._("* (Regex) ") + p_conf.StrPattern;
						case PatternType.Function:
							// TRANSLATORS: Bot message. SoundEffectListCommand plugin.
							return T._("* (Function) ") + p_conf.FunctionName;
					}
					return "";
				}).ToArray());
			}).Where(s => s.Length > 0).ToArray());
		}

		public override void Initialize(IPluginManager loader, IPlugin[] plugins) {}

		protected override bool DoExecute(int keywordIndex, IBot bot, IMessage message) {
			if (text.Length == 0) { Reload(bot); }
			if (text.Length == 0) {
				// TRANSLATORS: Bot message. SoundEffectListCommand plugin.
				bot.SendMessageAsync(message.Original.Channel, message.Original.Author.Mention + " " + T._("Sorry, I could not get information."));
			} else {
				bot.SendMessageAsync(message.Original.Channel, message.Original.Author.Mention + this.text);
			}
			message.Content = "";
			message.Terminated = true;
			message.AppliedPlugins.Add(this.Name);
			return true;
		}
	}
}
