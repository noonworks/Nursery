using Nursery.Utility;
using System;

namespace Nursery.Plugins.Schedules {
	public enum ScheduledMessageType {
		DoNothing,
		SendMessage,
		Talk,
	}

	public class ScheduledMessage {
		public ScheduledMessageType Type = ScheduledMessageType.DoNothing;
		public string Content = "";
		public string[] TextChannelIds = new string[] { };
		public bool CutIfTooLong = true;

		public ScheduledMessage Clone() {
			return new ScheduledMessage() {
				Type = this.Type,
				Content = this.Content,
				TextChannelIds = this.TextChannelIds,
				CutIfTooLong = this.CutIfTooLong
			};
		}
	}

	public abstract class ScheduledTaskBase : IScheduledTask {
		#region IScheduledTask
		public string Name { get; protected set; } = "";
		public bool Finished { get; protected set; } = false;

		public ScheduledTaskBase(string Name) {
			this.Name = Name;
		}
		#endregion
		
		public DateTime CheckedAt { get; protected set; }
		abstract protected bool DoCheck(IBot bot);
		abstract protected IScheduledTask[] DoExecute(IBot bot);

		protected bool Check(IBot bot) {
			var r = this.DoCheck(bot);
			if (r) { Logger.DebugLog("[SCHEDULE] " + this.Name + " going to be executed."); }
			return r;
		}

		public IScheduledTask[] Execute(IBot bot) {
			this.CheckedAt = DateTime.Now;
			if (! this.Check(bot)) {
				return new IScheduledTask[] { };
			}
			return this.DoExecute(bot);
		}
		
		private string[] ReplaceContent(string content, IBot bot, DateTime now) {
			var text1 = Nursery.Plugins.Utility.ReplaceDiscordValues(content, bot, now);
			var text2 = Nursery.Plugins.Utility.ReplaceDiscordValues(content.Replace("${announce}", "").Replace("${speak}", ""), bot, now);
			return new string[] { text1, text2 };
		}
		
		protected void Send(ScheduledMessage[] Messages, IBot bot) {
			var now = DateTime.Now;
			foreach (var message in Messages) {
				if (message == null) { continue; }
				if (message.Type == ScheduledMessageType.DoNothing) { continue; }
				if (message.Content.Length == 0) { continue; }
				var text = ReplaceContent(message.Content, bot, now);
				if (text.Length == 0) { continue; }
				if (message.Type == ScheduledMessageType.Talk) {
					bot.AddTalk(text[0], new TalkOptions());
					continue;
				}
				if (message.Type == ScheduledMessageType.SendMessage) {
					var channels = message.TextChannelIds;
					if (channels.Length == 1) {
						if (channels[0] == "default") {
							channels = null;
						} else if (channels[0] == "all") {
							channels = bot.TextChannelIdStrings;
						}
					}
					bot.SendMessageAsync(channels, text[0], text[1], message.CutIfTooLong);
					continue;
				}
			}
		}
	}
}
