using Nursery.Plugins;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Nursery.BasicPlugins {
	public class DiscordEmojiIdCommand : AbstractMentionKeywordCommand {
		public override string Name { get; } = "Nursery.BasicPlugins.DiscordEmojiIdCommand";
		// TRANSLATORS: Bot-Help message. DiscordEmojiIdCommand plugin.
		public override string HelpText { get; } = T._("Show discord emoji ID.") + " ```@voice-bot emojiid :any_discord_emoji:```";

		private static Regex EMOJI = new Regex(@"(\s*)<:([^\:]+):([0-9]+)>(\s*)");

		public DiscordEmojiIdCommand() : base(new string[] { "emojiid" }) { }

		public override void Initialize(IPluginManager loader, IPlugin[] plugins) { }

		protected override bool DoExecute(int keywordIndex, IBot bot, IMessage message) {
			List<string> ret = new List<string>();
			EMOJI.Replace(message.Content, (Match match) => {
				if (match.Groups.Count != 5) { return ""; }
				// TRANSLATORS: Bot message. DiscordEmojiIdCommand plugin. {0} is Emoji name. {1} is Emoji Id.
				ret.Add(T._(":{0}: is {1}", match.Groups[2].Value, match.Groups[3].Value));
				return "";
			});
			var msg = String.Join("\n", ret);
			if (msg.Length > 0) {
				bot.SendMessageAsync(message.Original.Channel, message.Original.Author, "\n" + msg, false);
			}
			message.Content = "";
			message.Terminated = true;
			message.AppliedPlugins.Add(this.Name);
			return true;
		}
	}
}
