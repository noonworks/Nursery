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
		
		private static readonly string[] DTFormatStrings = new string[] { "yyyy", "yy", "MMMM", "MM", "M", "dddd", "ddd", "dd", "d", "HH", "H", "mm", "m" };
		private const string UserNameRegex = @"\${username([0-9]+)}";
		private const string NickNameRegex = @"\${nickname([0-9]+)}";
		private string[] ReplaceContent(string content, IBot bot, DateTime now) {
			var text = content;
			// replace now datetime
			foreach (var dts in DTFormatStrings) {
				text = text.Replace("${" + dts + "}", now.ToString(dts.Length == 1 ? "%" + dts : dts));
			}
			// replace username and nickname
			text = Regex.Replace(text, UserNameRegex, m => bot.GetUserName(m.Groups[1].Value));
			text = Regex.Replace(text, NickNameRegex, m => bot.GetNickName(m.Groups[1].Value));
			// replace announce or speak flag
			var text1 = text.Replace("${announce}", bot.AnnounceLabel).Replace("${speak}", bot.SpeakLabel);
			var text2 = text.Replace("${announce}", "").Replace("${speak}", "");
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
