using System;
using System.Linq;
using Nursery.Plugins;

namespace Nursery.BasicPlugins {
	public class HelpCommand : AbstractMentionKeywordCommand {
		public override string Name { get; } = "Nursery.BasicPlugins.HelpCommand";
		public override string HelpText { get => this.help_text; }

		// TRANSLATORS: Bot-Help message. HelpText plugin.
		private string help_text = T._("Show command and filter help.") + " ```@voice-bot help\n@voice-bot help-command\n@voice-bot help-filter```";
		private string command_help = "";
		private string filter_help = "";
		private bool replaced = false;

		public HelpCommand() : base(new string[] { "help-command", "help-filter", "help" }) { }

		public override void Initialize(IPluginManager loader, IPlugin[] plugins) {
			// TRANSLATORS: Bot message. HelpText plugin.
			command_help = "\n" + T._("[COMMANDS]") + "\n" + String.Join("\n", plugins.Where(p => p.Type == Plugins.Type.Command).Select(p => {
				return "* " + p.HelpText;
			}).ToArray());
			// TRANSLATORS: Bot message. HelpText plugin.
			filter_help = "\n" + T._("[FILTERS]") + "\n" + String.Join("\n", plugins.Where(p => p.Type == Plugins.Type.Filter).Select(p => {
				return "* " + p.HelpText;
			}).ToArray());
		}

		protected override bool DoExecute(int keywordIndex, IBot bot, IMessage message) {
			if (!replaced) {
				var mn = bot.Username;
				if (mn.Length > 0) {
					this.command_help = this.command_help.Replace("voice-bot", mn);
					this.filter_help = this.filter_help.Replace("voice-bot", mn);
					this.help_text = this.help_text.Replace("voice-bot", mn);
					this.replaced = true;
				}
			}
			switch (keywordIndex) {
				case 0:
					bot.SendMessageAsync(message.Original.Channel, message.Original.Author, this.command_help, false);
					break;
				case 1:
					bot.SendMessageAsync(message.Original.Channel, message.Original.Author, this.filter_help, false);
					break;
				case 2:
					bot.SendMessageAsync(message.Original.Channel, message.Original.Author, this.HelpText, false);
					break;
			}
			message.Content = "";
			message.Terminated = true;
			message.AppliedPlugins.Add(this.Name);
			return true;
		}
	}
}
