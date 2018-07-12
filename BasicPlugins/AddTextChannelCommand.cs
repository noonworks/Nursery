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
					// TRANSLATORS: Bot message. AddTextChannelCommand plugin. {0} is mention part. {1} is name of text channel.
					bot.SendMessageAsync(message.Original.Channel, T._("{0}Text channel [{1}] is already added.", message.Original.Author.Mention + " ", message.Original.Channel.Name));
					break;
				case AddChannelResult.Succeed:
					// TRANSLATORS: Bot message. AddTextChannelCommand plugin. {0} is mention part. {1} is name of text channel.
					bot.SendMessageAsync(message.Original.Channel, T._("{0}Added text channel [{1}].", "", message.Original.Channel.Name));
					break;
				case AddChannelResult.NotJoined:
					// TRANSLATORS: Bot message. AddTextChannelCommand plugin. {0} is mention part.
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
