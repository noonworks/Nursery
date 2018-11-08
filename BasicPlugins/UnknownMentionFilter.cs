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
			if (message.Original.Author.IsBot) { return false; }
			if (message.AppliedPlugins.Contains("Nursery.UserDefinedFilterPlugin.UserDefinedFilter")) { return false; }
			var reply = Utility.Messages.TrimMention(message.Content);
			// TRANSLATORS: Bot message. UnknownMentionFilter plugin. If it is longer than DISCORD_MESSAGE_MAX, it will be cut.
			bot.SendMessageAsync(message.Original.Channel, message.Original.Author, T._("Sorry, I could not understand the command.")
				+ "\n```\n" + reply.Trimmed + "\n```", true);
			message.Content = "";
			message.Terminated = true;
			message.AppliedPlugins.Add(this.Name);
			return true;
		}
	}
}
