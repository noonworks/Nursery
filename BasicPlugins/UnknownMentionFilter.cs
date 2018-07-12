using Nursery.Plugins;

namespace Nursery.BasicPlugins {
	public class UnknownMentionFilter : IPlugin {
		public string Name { get; } = "Nursery.BasicPlugins.UnknownMentionFilter";
		// TRANSLATORS: Bot-Help message. UnknownMentionFilter plugin.
		public string HelpText { get; } = T._("Filter to ignore mentions that voice-bot could not been understood.");
		Plugins.Type IPlugin.Type => Plugins.Type.Filter;

		public void Initialize(IPluginManager loader, IPlugin[] plugins) { }

		public bool Execute(IBot bot, IMessage message) {
			if (message.Content.Length == 0) { return false; }
			if (!Nursery.Utility.Messages.IsMentionFor(bot.Id, message.Original)) { return false; }
			var reply = Utility.Messages.TrimMention(message.Content);
			// TRANSLATORS: Bot message. UnknownMentionFilter plugin. {0} is mention part.
			bot.SendMessageAsync(message.Original.Channel, T._("{0}Sorry, I could not understand the command.", message.Original.Author.Mention +" ")
				+ "\n```\n" + reply.Trimmed + "\n```");
			message.Content = "";
			message.Terminated = true;
			message.AppliedPlugins.Add(this.Name);
			return true;
		}
	}
}
