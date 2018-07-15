using System;
using System.Linq;
using Nursery.Plugins;
using Nursery.Utility;

namespace Nursery.SoundEffectPlugin {
	public class SoundEffectListCommand : AbstractMentionKeywordCommand {
		public override string Name { get; } = "Nursery.SoundEffectPlugin.SoundEffectListCommand";
		// TRANSLATORS: Bot-Help message. SoundEffectListCommand plugin.
		public override string HelpText { get; } = T._("Show sound list.") + " ```@voice-bot soundlist\n@voice-bot selist```";

		private string text_se = "";
		private string text_keywords = "";

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
			this.text_se = "\n\n" + T._("[`se NAME` or `sound NAME`]") + "\n";
			this.text_se += String.Join(", ", sound_configs.Select(s_conf => String.Join(", ", s_conf.Aliases)).Where(s => s.Length > 0).ToArray());
			// TRANSLATORS: Bot message. SoundEffectListCommand plugin.
			this.text_keywords = "\n\n" + T._("[Other keywords]") + "\n";
			this.text_keywords += String.Join("\n", sound_configs.Select(s_conf => {
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
			if (text_se.Length == 0) { Reload(bot); }
			if (text_se.Length == 0) {
				// TRANSLATORS: Bot message. SoundEffectListCommand plugin. If it is longer than DISCORD_MESSAGE_MAX, it will be cut.
				bot.SendMessageAsync(message.Original.Channel, message.Original.Author, T._("Sorry, I could not get information."), true);
			} else {
				if (this.text_se.Length + this.text_keywords.Length + message.Original.Author.Mention.Length > Common.DISCORD_MAX_MESSAGE_LENGTH - 1) {
					bot.SendMessageAsync(message.Original.Channel, message.Original.Author, this.text_se, false);
					bot.SendMessageAsync(message.Original.Channel, message.Original.Author, this.text_keywords, false);
				} else {
					bot.SendMessageAsync(message.Original.Channel, message.Original.Author, this.text_se + this.text_keywords, true);
				}
			}
			message.Content = "";
			message.Terminated = true;
			message.AppliedPlugins.Add(this.Name);
			return true;
		}
	}
}
