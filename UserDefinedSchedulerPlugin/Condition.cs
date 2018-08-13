using Newtonsoft.Json;
using Nursery.Utility;
using System;

namespace Nursery.UserDefinedSchedulerPlugin {
	public enum ConditionType {
		Unknown,
		DateTime,
		Interval,
		Function,
		TimeRange,
	}

	public enum IntervalConditionStartOption {
		RunOnJoin,
		RunNext,
		StartAt,
		NotStart,
	}

	[JsonObject("Nursery.UserDefinedSchedulerPlugin.Condition")]
	public class Condition {
		[JsonProperty("type")]
		public string TypeStr { get; set; } = "";
		[JsonIgnore]
		public ConditionType Type { get; private set; } = ConditionType.Unknown;
		[JsonIgnore]
		public bool Valid { get; private set; } = false;

		#region DateTime condition
		[JsonProperty("date_time_pattern")]
		public string DateTimePattern { get; set; } = "";
		[JsonIgnore]
		public DateTimeMatcher DateTimeMatcher { get; private set; } = null;
		#endregion

		#region Interval condition
		[JsonProperty("interval_minutes")]
		public long IntervalMinutes { get; set; } = 0;
		[JsonProperty("interval_start_option")]
		public string IntervalStartOptionStr { get; set; } = "";
		[JsonIgnore]
		public IntervalConditionStartOption IntervalStartOption { get; private set; } = IntervalConditionStartOption.NotStart;
		[JsonProperty("interval_start_at")]
		public string IntervalStartAtStr { get; set; } = "";
		[JsonIgnore]
		public DateTime? IntervalStartAt { get; private set; } = null;
		#endregion

		#region Function condition
		[JsonProperty("function_str")]
		public string FunctionStr { get; set; } = "";
		[JsonProperty("function_name")]
		public string FunctionName { get; set; } = "";
		[JsonIgnore]
		public JSScheduleConditionFunction Function { get; private set; } = null;
		#endregion

		#region Range
		[JsonProperty("time_range_date_pattern")]
		public string TimeRangeDatePattern { get; set; } = "";
		[JsonProperty("time_range_start")]
		public int TimeRangeStart { get; set; } = -1;
		[JsonProperty("time_range_end")]
		public int TimeRangeEnd { get; set; } = -1;
		[JsonProperty("time_range_use_previous_day")]
		public bool TimeRangeUsePreviousDay { get; set; } = false;
		[JsonIgnore]
		public DateTimeRangeMatcher TimeRangeMatcher { get; private set; } = null;
		#endregion

		private void SetType() {
			switch (this.TypeStr.ToLower()) {
				case "date_time":
					this.Type = ConditionType.DateTime;
					break;
				case "interval":
					this.Type = ConditionType.Interval;
					break;
				case "function":
					this.Type = ConditionType.Function;
					break;
				case "time_range":
					this.Type = ConditionType.TimeRange;
					break;
				default:
					this.Type = ConditionType.Unknown;
					break;
			}
		}

		private bool SetupDateTime() {
			this.DateTimeMatcher = new DateTimeMatcher(this.DateTimePattern);
			return this.DateTimeMatcher.Valid;
		}

		private bool SetupInterval() {
			switch (this.IntervalStartOptionStr.ToLower()) {
				case "run_next":
					this.IntervalStartOption = IntervalConditionStartOption.RunNext;
					break;
				case "run_on_join":
					this.IntervalStartOption = IntervalConditionStartOption.RunOnJoin;
					break;
				case "start_at":
					this.IntervalStartOption = IntervalConditionStartOption.StartAt;
					this.IntervalStartAt = DateTimeMatcher.ParseDateTimeString(this.IntervalStartAtStr);
					break;
				case "not_start":
				default:
					this.IntervalStartOption = IntervalConditionStartOption.NotStart;
					break;
			}
			if (this.IntervalStartOption == IntervalConditionStartOption.StartAt && this.IntervalStartAt == null) {
				return false;
			}
			return true;
		}

		private bool SetupFunction() {
			if (this.FunctionName.Length == 0 || this.FunctionStr.Length == 0) {
				return false;
			}
			JSWrapper.Instance.SetFunction(this.FunctionName, this.FunctionStr);
			this.Function = (JSScheduleArgument arg) => {
				try {
					var r = JSWrapper.Instance.ExecuteFunction(FunctionName, arg);
					if (r == null) { return false; }
					if (r.GetType() == typeof(bool)) { return (bool)r; }
					return false;
				} catch (System.Exception e) {
					// TRANSLATORS: Log message. UserDefinedScheduler plugin.
					Logger.Log(T._("Could not get JS result."));
					Logger.DebugLog(e.ToString());
					return false;
				}
			};
			return true;
		}

		private bool SetupRange() {
			this.TimeRangeMatcher = new DateTimeRangeMatcher(this.TimeRangeStart, this.TimeRangeEnd, this.TimeRangeDatePattern, this.TimeRangeUsePreviousDay);
			return this.TimeRangeMatcher.Valid;
		}

		public void Init() {
			SetType();
			switch (this.Type) {
				case ConditionType.DateTime:
					this.Valid = SetupDateTime();
					break;
				case ConditionType.Interval:
					this.Valid = SetupInterval();
					break;
				case ConditionType.Function:
					this.Valid = SetupFunction();
					break;
				case ConditionType.TimeRange:
					this.Valid = SetupRange();
					break;
				case ConditionType.Unknown:
				default:
					this.Valid = false;
					break;
			}
		}
	}

	public delegate bool JSScheduleConditionFunction(JSScheduleArgument arg);
}
