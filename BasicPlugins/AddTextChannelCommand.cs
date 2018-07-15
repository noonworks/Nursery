using Nursery.Plugins;

namespace Nursery.BasicPlugins {
	class AddTextChannelCommand : AbstractMentionKeywordCommand {
		public override string Name { get; } = "Nursery.BasicPlugins.AddTextChannelCommand";
		// TRANSLATORS: Bot-Help message. AddTextChannelCommand plugin.
		public override string HelpText { get; } = T._("Add voice-bot to text channel.") + " ```@voice-bot addchannel```";

		public AddTextChannelCommand() : base(new string[] { "addchannel" }) { }

		public override void Initialize(IPluginManager loader, IPlugin[] plugins) { }

		protected override bool DoExecute(int keywordIndex, IBot bot, IMessage message) {
			switch (bot.AddChannel(message)) {
				case AddChannelResult.AlreadyAdded:
					// TRANSLATORS: Bot message. AddTextChannelCommand plugin. {0} is name of text channel. If it is longer than DISCORD_MESSAGE_MAX, it will be cut.
					bot.SendMessageAsync(message.Original.Channel, message.Original.Author, T._("Text channel [{0}] is already added.", message.Original.Channel.Name), true);
					break;
				case AddChannelResult.Succeed:
					// TRANSLATORS: Bot message. AddTextChannelCommand plugin. {0} is name of text channel. If it is longer than DISCORD_MESSAGE_MAX, it will be cut.
					bot.SendMessageAsync(message.Original.Channel, T._("Added text channel [{0}].", message.Original.Channel.Name), true);
					break;
				case AddChannelResult.NotJoined:
					// TRANSLATORS: Bot message. AddTextChannelCommand plugin. If it is longer than DISCORD_MESSAGE_MAX, it will be cut.
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
