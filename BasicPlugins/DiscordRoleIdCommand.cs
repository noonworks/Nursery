using Discord.WebSocket;
using Nursery.Plugins;
using System;
using System.Collections.Generic;

namespace Nursery.BasicPlugins {
	public class DiscordRoleIdCommand : AbstractMentionKeywordCommand {
		public override string Name { get; } = "Nursery.BasicPlugins.DiscordRoleIdCommand";
		// TRANSLATORS: Bot-Help message. DiscordRoleIdCommand plugin.
		public override string HelpText { get; } = T._("Show discord Role ID.") + " ```@voice-bot roleid```";

		public DiscordRoleIdCommand() : base(new string[] { "roleid" }) { }

		public override void Initialize(IPluginManager loader, IPlugin[] plugins) { }

		protected override bool DoExecute(int keywordIndex, IBot bot, IMessage message) {
			List<string> ret = new List<string>();
			var user = (message.Original.Author as SocketGuildUser);
			foreach (var role in user.Roles) {
				// TRANSLATORS: Bot message. DiscordRoleIdCommand plugin. {0} is Role name. {1} is Role Id.
				ret.Add(T._("{0} is {1}", role.Name, role.Id));
			}
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
