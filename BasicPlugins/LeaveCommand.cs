using Nursery.Plugins;

namespace Nursery.BasicPlugins {
	public class LeaveCommand : AbstractMentionKeywordCommand {
		public override string Name { get; } = "Nursery.BasicPlugins.LeaveCommand";
		// TRANSLATORS: Bot-Help message. LeaveCommand plugin.
		public override string HelpText { get; } = T._("Remove voice-bot from channel.") + " ```@voice-bot leave\n@voice-bot bye\n@voice-bot disconnect\n@voice-bot kick```";

		public LeaveCommand() : base(new string[] { "leave", "disconnect", "kick", "bye" }) { }

		public override void Initialize(IPluginManager loader, IPlugin[] plugins) { }

		protected override bool DoExecute(int keywordIndex, IBot bot, IMessage message) {
			switch (bot.LeaveChannel(message)) {
				case LeaveChannelResult.NotJoined:
					// TRANSLATORS: Bot message. LeaveCommand plugin. If it is longer than DISCORD_MESSAGE_MAX, it will be cut.
					bot.SendMessageAsync(message.Original.Channel, message.Original.Author, T._("Oh? I'm not in any channels..."), true);
					break;
				case LeaveChannelResult.Succeed:
					// TRANSLATORS: Bot message. LeaveCommand plugin. If it is longer than DISCORD_MESSAGE_MAX, it will be cut.
					bot.SendMessageAsync(message.Original.Channel, T._("Bye!"), true);
					break;
			}
			message.Content = "";
			message.Terminated = true;
			message.AppliedPlugins.Add(this.Name);
			return true;
		}
	}
}
