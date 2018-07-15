using Nursery.Plugins;

namespace Nursery.BasicPlugins {
	class RemoveTextChannelCommand : AbstractMentionKeywordCommand {
		public override string Name { get; } = "Nursery.BasicPlugins.RemoveTextChannelCommand";
		// TRANSLATORS: Bot-Help message. RemoveTextChannelCommand plugin.
		public override string HelpText { get; } = T._("Remove voice-bot from text channel.") + " ```@voice-bot removechannel```";

		public RemoveTextChannelCommand() : base(new string[] { "removechannel" }) { }

		public override void Initialize(IPluginManager loader, IPlugin[] plugins) { }

		protected override bool DoExecute(int keywordIndex, IBot bot, IMessage message) {
			switch (bot.RemoveChannel(message)) {
				case RemoveChannelResult.AlreadyRemoved:
					// TRANSLATORS: Bot message. RemoveTextChannelCommand plugin. {0} is name of text channel. If it is longer than DISCORD_MESSAGE_MAX, it will be cut.
					bot.SendMessageAsync(message.Original.Channel, message.Original.Author, T._("Text channel [{0}] is not added.", message.Original.Channel.Name), true);
					break;
				case RemoveChannelResult.Succeed:
					// TRANSLATORS: Bot message. RemoveTextChannelCommand plugin. {0} is name of text channel. If it is longer than DISCORD_MESSAGE_MAX, it will be cut.
					bot.SendMessageAsync(message.Original.Channel, T._("Removed text channel [{0}].", "", message.Original.Channel.Name), true);
					break;
				case RemoveChannelResult.NotJoined:
					// TRANSLATORS: Bot message. RemoveTextChannelCommand plugin. If it is longer than DISCORD_MESSAGE_MAX, it will be cut.
					bot.SendMessageAsync(message.Original.Channel, message.Original.Author, T._("Oh? I'm not in any channels..."), true);
					break;
			}
			message.Content = "";
			message.Terminated = true;
			message.AppliedPlugins.Add(this.Name);
			return true;
		}
	}
}
