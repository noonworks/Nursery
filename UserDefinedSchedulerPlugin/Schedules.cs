using Nursery.Plugins;
using Nursery.Plugins.Schedules;
using Nursery.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nursery.UserDefinedSchedulerPlugin {
	#region Conditions
	abstract class ConditionBase {
		public ConditionType Type { get; protected set; }
		protected Condition Config { get; }
		public bool Valid { get; protected set; } = true;
		public DateTime LastExecute = DateTime.MinValue;

		public ConditionBase(Condition condition) {
			this.Config = condition;
			this.Type = this.Config.Type;
		}

		abstract public bool Check(IBot bot, UserDefinedScheduledTask schedule);
	}

	class IntervalCondition : ConditionBase {
		public DateTime StartAt = DateTime.MinValue;
		public long Interval { get => this.Config.IntervalMinutes; }

		public IntervalCondition(Condition condition) : base(condition) {
			if (this.Config.Type != ConditionType.Interval) {
				this.Valid = false;
				return;
			}
			if (this.Config.IntervalStartOption == IntervalConditionStartOption.StartAt) {
				if (this.Config.IntervalStartAt == null) {
					this.Valid = false;
					return;
				}
				this.StartAt = (DateTime)this.Config.IntervalStartAt;
				this.StartAt = this.StartAt.TruncateSeconds(); // YYYY/MMD/DD hh:mm:00.0
			}
		}

		private bool SetLastExecutedFromStartAt(DateTime dt) {
			this.LastExecute = DateTime.MinValue;
			var tmp = this.StartAt;
			while (tmp <= dt) {
				this.LastExecute = tmp;
				tmp = tmp.AddMinutes(this.Config.IntervalMinutes);
			}
			return (this.LastExecute.Equals(dt));
		}

		public override bool Check(IBot bot, UserDefinedScheduledTask schedule) {
			// first time
			if (this.LastExecute == DateTime.MinValue) {
				switch (this.Config.IntervalStartOption) {
					case IntervalConditionStartOption.RunOnJoin:
						this.StartAt = schedule.CheckedAt;
						this.LastExecute = schedule.CheckedAt;
						return true;
					case IntervalConditionStartOption.RunNext:
						this.StartAt = schedule.CheckedAt;
						this.LastExecute = schedule.CheckedAt;
						return false;
					case IntervalConditionStartOption.StartAt:
						return SetLastExecutedFromStartAt(schedule.CheckedAt);
					case IntervalConditionStartOption.NotStart:
					default:
						return false;
				}
			}
			// other than first time
			if ((schedule.CheckedAt - this.LastExecute).TotalMinutes >= this.Config.IntervalMinutes) {
				this.LastExecute = schedule.CheckedAt;
				return true;
			}
			return false;
		}
	}

	class DateTimeCondition : ConditionBase {
		public DateTimeCondition(Condition condition) : base(condition) {
			if (this.Config.Type != ConditionType.DateTime) {
				this.Valid = false;
				return;
			}
		}

		public override bool Check(IBot bot, UserDefinedScheduledTask schedule) {
			return this.Config.DateTimeMatcher.IsMatch(schedule.CheckedAt, true);
		}
	}

	class FunctionCondition : ConditionBase {
		public FunctionCondition(Condition condition) : base(condition) {
			if (this.Config.Type != ConditionType.Function) {
				this.Valid = false;
				return;
			}
		}

		public override bool Check(IBot bot, UserDefinedScheduledTask schedule) {
			return this.Config.Function(new JSScheduleArgument(bot, schedule));
		}
	}

	class TimeRangeCondition : ConditionBase {
		private DateTime PreviousBase = DateTime.MinValue;

		public TimeRangeCondition(Condition condition) : base(condition) {
			if (this.Config.Type != ConditionType.TimeRange) {
				this.Valid = false;
				return;
			}
			this.Valid = this.Config.TimeRangeMatcher.Valid;
		}

		public override bool Check(IBot bot, UserDefinedScheduledTask schedule) {
			var result = this.Config.TimeRangeMatcher.Match(schedule.CheckedAt);
			if (result.Success) {
				if (PreviousBase == result.BaseDate) { return false; }
				PreviousBase = result.BaseDate.Date;
			}
			return result.Success;
		}
	}
	#endregion

	#region Processes
	abstract class ProcessBase {
		protected Process Config { get; }
		public bool Valid { get; protected set; } = true;

		public ProcessBase(Process process) {
			this.Config = process;
		}

		abstract public ScheduledMessage[] Execute(IBot bot, UserDefinedScheduledTask schedule);
	}

	class SendMessageProcess : ProcessBase {
		private ScheduledMessage[] Messages { get; set; }

		public SendMessageProcess(Process process) : base(process) {
			if (this.Config.Type != ProcessType.SendMessage) {
				this.Valid = false;
			}
			this.Messages = this.Config.Values.Select(v => {
				if (v.Length == 0) { return null; }
				return new ScheduledMessage() {
					Type = ScheduledMessageType.SendMessage,
					Content = v,
					TextChannelIds = this.Config.SendTo,
					CutIfTooLong = this.Config.CutIfTooLong
				};
			}).Where(m => m != null).ToArray();
		}

		public override ScheduledMessage[] Execute(IBot bot, UserDefinedScheduledTask schedule) {
			return this.Messages.Select(m => m.Clone()).ToArray();
		}
	}

	class TalkProcess : ProcessBase {
		private ScheduledMessage[] Messages { get; set; }

		public TalkProcess(Process process) : base(process) {
			if (this.Config.Type != ProcessType.Talk) {
				this.Valid = false;
			}
			this.Messages = this.Config.Values.Select(v => {
				if (v.Length == 0) { return null; }
				return new ScheduledMessage() {
					Type = ScheduledMessageType.Talk,
					Content = v
				};
			}).Where(m => m != null).ToArray();
		}

		public override ScheduledMessage[] Execute(IBot bot, UserDefinedScheduledTask schedule) {
			return this.Messages.Select(m => m.Clone()).ToArray();
		}
	}

	class FunctionProcess : ProcessBase {
		public FunctionProcess(Process process) : base(process) {
			if (this.Config.Type != ProcessType.Function) {
				this.Valid = false;
			}
		}

		public override ScheduledMessage[] Execute(IBot bot, UserDefinedScheduledTask schedule) {
			return this.Config.Function(new JSScheduleArgument(bot, schedule));
		}
	}
	#endregion

	public class UserDefinedScheduledTask : ScheduledTaskBase, IJSScheduler {
		private ScheduleConfig Config { get; }
		private ConditionBase[] Conditions;
		private ProcessBase[] Processes;
		private bool Joined = false;

		#region IJSScheduler
		public bool Valid { get; private set; } = true;
		// DateTime CheckedAt { get; } in base class
		public string AdditionalData { get; set; } = null;
		public DateTime LastExecute { get; protected set; } = DateTime.MinValue;
		public long TotalCount { get; protected set; } = 0;
		public long Count { get; protected set; } = 0;
		public long Interval { get; protected set; } = 0;
		#endregion

		public UserDefinedScheduledTask(ScheduleConfig config) : base("dummy") {
			this.Config = config;
			this.Name = UserDefinedScheduler.NAME + "." + this.Config.Path;
			this.TotalCount = 0;
			this.Count = 0;
			Setup();
			if (this.Conditions.Length == 0 || this.Processes.Length == 0) {
				this.Valid = false;
			}
		}

		private void Setup() {
			this.Conditions = this.Config.Conditions.Select<Condition, ConditionBase>(condition => {
				switch (condition.Type) {
					case ConditionType.DateTime:
						return new DateTimeCondition(condition);
					case ConditionType.Interval:
						return new IntervalCondition(condition);
					case ConditionType.Function:
						return new FunctionCondition(condition);
					case ConditionType.TimeRange:
						return new TimeRangeCondition(condition);
					case ConditionType.Unknown:
					default:
						return null;
				}
			}).Where(c => c != null && c.Valid).ToArray();
			this.Processes = this.Config.Processes.Select<Process, ProcessBase>(process => {
				switch (process.Type) {
					case ProcessType.SendMessage:
						return new SendMessageProcess(process);
					case ProcessType.Talk:
						return new TalkProcess(process);
					case ProcessType.Function:
						return new FunctionProcess(process);
					case ProcessType.Unknown:
					default:
						return null;
				}
			}).Where(c => c != null && c.Valid).ToArray();
			// check interval
			var intervals = this.Conditions.Where(c => c.Type == ConditionType.Interval).ToArray();
			if (intervals.Length == 1) {
				this.Interval = ((IntervalCondition)intervals[0]).Interval;
			}
		}

		protected override bool DoCheck(IBot bot) {
			if (!this.Valid) { return false; }
			// truncate seconds and milliseconds
			this.CheckedAt = this.CheckedAt.TruncateSeconds();
			// bot is not joined
			if (!bot.IsJoined) {
				if (this.Joined) {
					foreach (var c in this.Conditions) {
						c.LastExecute = DateTime.MinValue;
					}
					this.Joined = false;
					this.LastExecute = DateTime.MinValue;
					this.Count = 0;
				}
				return false;
			}
			// bot is joined
			if (!this.Joined) { this.Joined = true; }
			var established = false;
			foreach (var c in this.Conditions) {
				if (c.Check(bot, this)) {
					established = true;
					break;
				}
			}
			if (established) {
				foreach (var c in this.Conditions) {
					c.LastExecute = this.CheckedAt;
				}
				this.LastExecute = this.CheckedAt;
			}
			return established;
		}

		protected override IScheduledTask[] DoExecute(IBot bot) {
			if (!this.Valid) { return null; }
			this.Count++;
			this.TotalCount++;
			var messages = new List<ScheduledMessage>();
			foreach (var p in this.Processes) {
				var r = p.Execute(bot, this);
				if (r != null && r.Length > 0) { messages.AddRange(r); }
			}
			if (messages.Count > 0) {
				var arr = messages.Select(m => {
					m.Content = m.Content.Replace("${total_count}", this.TotalCount.ToString()).Replace("${count}", this.Count.ToString());
					if (Interval > 0) {
						m.Content = m.Content.Replace("${interval}", Interval.ToString())
							.Replace("${total_count_x_interval}", (this.TotalCount * Interval).ToString())
							.Replace("${count_x_interval}", (this.Count * Interval).ToString());
					}
					return m;
				}).ToArray();
				Send(arr, bot);
			}
			return null;
		}
	}
}
