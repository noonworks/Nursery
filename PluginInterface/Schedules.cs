using System;

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
			return r.Established;
		}

		public IScheduledTask[] Execute(IBot bot) {
			if (! this.Check(this.CheckedAt, bot)) {
				return new IScheduledTask[] { };
			}
			return this.DoExecute(bot);
		}
	}
}
