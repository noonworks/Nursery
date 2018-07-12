using Nursery.Plugins;

namespace Nursery.BasicPlugins {
	public class JoinCommand : AbstractMentionKeywordCommand {
		public override string Name { get; } = "Nursery.BasicPlugins.JoinCommand";
		// TRANSLATORS: Bot-Help message. JoinCommand plugin.
		public override string HelpText { get; } = T._("Join voice-bot to channel.") + " ```@voice-bot join\n@voice-bot come\n@voice-bot connect\n@voice-bot here```";

		public JoinCommand() : base(new string[] { "join", "connect", "here", "come" }) { }

		public override void Initialize(IPluginManager loader, IPlugin[] plugins) { }

		protected override bool DoExecute(int keywordIndex, IBot bot, IMessage message) {
			var ret = bot.JoinChannel(message);
			switch (ret.State) {
				case JoinChannelState.AlreadyJoined:
					// TRANSLATORS: Bot message. JoinCommand plugin. {0} is mention part.
					bot.SendMessageAsync(message.Original.Channel, T._(
						"{0}Oh? I'm already connected to the channel. Please let me bye, and call me again.",
						message.Original.Author.Mention + " "));
					break;
				case JoinChannelState.WhereYouAre:
					// TRANSLATORS: Bot message. JoinCommand plugin. {0} is mention part.
					bot.SendMessageAsync(message.Original.Channel, T._(
						"{0}Where should I go? Please connect to the voice channel, and call me again.",
						message.Original.Author.Mention + " "));
					break;
				case JoinChannelState.Succeed:
					// TRANSLATORS: Bot message. JoinCommand plugin. {0} is text channel. {1} is voice channel.
					bot.SendMessageAsync(message.Original.Channel, T._(
						"Set text channel to [{0}], voice channel to [{1}].", message.Original.Channel.Name, ret.VoiceChannelName));
					break;
			}
			message.Content = "";
			message.Terminated = true;
			message.AppliedPlugins.Add(this.Name);
			return true;
		}
	}
}
