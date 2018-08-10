using Newtonsoft.Json;
using Nursery.Utility;
using System;

namespace Nursery.UserDefinedSchedulerPlugin {
	public enum ConditionType {
		Unknown,
		DateTime,
		Interval,
		Function,
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
		[JsonProperty("date_time_run_on_start")]
		public bool DateTimeRunOnStart { get; set; } = false;
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

		public void Init() {

		}
	}

	public delegate bool JSScheduleConditionFunction(JSScheduleArgument arg);
}
