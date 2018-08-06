using Nursery.Utility;
using System;
using System.Text.RegularExpressions;

namespace Nursery.Plugins.Schedules {
	public class ScheduleCheckResult {
		public bool Established = false;
		public string AdditionalData = null;
	}

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
	}

	public abstract class ScheduledTaskBase : IScheduledTask {
		#region IScheduledTask
		public string Name { get; protected set; } = "";
		public bool Finished { get; protected set; } = false;

		public ScheduledTaskBase(string Name) {
			this.Name = Name;
		}
		#endregion

		protected string AdditionalData { get; set; } = null;
		protected DateTime CheckedAt { get; set; }
		abstract protected ScheduleCheckResult DoCheck(IBot bot);
		abstract protected IScheduledTask[] DoExecute(IBot bot);

		protected bool Check(DateTime checkedAt, IBot bot) {
			this.CheckedAt = checkedAt;
			var r = this.DoCheck(bot);
			if (r.AdditionalData != null) {
				this.AdditionalData = r.AdditionalData;
			}
			if (r.Established) { Logger.DebugLog("[SCHEDULE] " + this.Name + " going to be executed."); }
			return r.Established;
		}

		public IScheduledTask[] Execute(IBot bot) {
			if (! this.Check(this.CheckedAt, bot)) {
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
				if (message.Type == ScheduledMessageType.DoNothing) { continue; }
				if (message.Content.Length == 0) { continue; }
				var text = ReplaceContent(message.Content, bot, now);
				if (text.Length == 0) { continue; }
				if (message.Type == ScheduledMessageType.Talk) {
					bot.AddTalk(text[0], new TalkOptions());
					continue;
				}
				if (message.Type == ScheduledMessageType.SendMessage) {
					bot.SendMessageAsync(message.TextChannelIds, text[0], text[1], message.CutIfTooLong);
					continue;
				}
			}
		}
	}
}
