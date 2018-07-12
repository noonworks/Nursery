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
					// TRANSLATORS: Bot message. RemoveTextChannelCommand plugin. {0} is mention part. {1} is name of text channel.
					bot.SendMessageAsync(message.Original.Channel, T._("{0}Text channel [{1}] is not added.", message.Original.Author.Mention + " ", message.Original.Channel.Name));
					break;
				case RemoveChannelResult.Succeed:
					// TRANSLATORS: Bot message. RemoveTextChannelCommand plugin. {0} is mention part. {1} is name of text channel.
					bot.SendMessageAsync(message.Original.Channel, T._("{0}Removed text channel [{1}].", "", message.Original.Channel.Name));
					break;
				case RemoveChannelResult.NotJoined:
					// TRANSLATORS: Bot message. RemoveTextChannelCommand plugin. {0} is mention part.
					bot.SendMessageAsync(message.Original.Channel, T._("{0}Oh? I'm not in any channels...", message.Original.Author.Mention + " "));
					break;
			}
			message.Content = "";
			message.Terminated = true;
			message.AppliedPlugins.Add(this.Name);
			return true;
		}
	}
}
