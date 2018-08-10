using Newtonsoft.Json;
using Nursery.Plugins.Schedules;

namespace Nursery.UserDefinedSchedulerPlugin {
	public enum ProcessType {
		Unknown,
		SendMessage,
		Talk,
		Function,
	}

	public enum SendToType {
		defaultChannel,
		allChannels,
		specifiedChannels,
	}

	[JsonObject("Nursery.UserDefinedSchedulerPlugin.Process")]
	public class Process {
		[JsonProperty("type")]
		public string TypeStr { get; set; } = "";
		[JsonIgnore]
		public ProcessType Type { get; private set; } = ProcessType.Unknown;
		[JsonIgnore]
		public bool Valid { get; private set; } = false;

		#region SendMessage or Talk
		[JsonProperty("values")]
		public string[] Values { get; set; } = new string[] { };
		#endregion

		#region SendMessage
		[JsonProperty("send_to_type")]
		public string SendToTypeStr { get; set; } = "default";
		[JsonIgnore]
		public SendToType SendToType { get; set; } = SendToType.defaultChannel;
		[JsonProperty("send_to")]
		public string[] SendTo { get; set; } = new string[] { };
		[JsonProperty("cut_if_too_long")]
		public bool CutIfTooLong { get; set; } = true;
		#endregion

		#region Function
		[JsonProperty("function_str")]
		public string FunctionStr { get; set; } = "";
		[JsonProperty("function_name")]
		public string FunctionName { get; set; } = "";
		[JsonIgnore]
		public JSScheduleProcessFunction Function { get; private set; } = null;
		#endregion

		public void Init() {

		}
	}

	public delegate ScheduledMessage[] JSScheduleProcessFunction(JSScheduleArgument arg);
}
